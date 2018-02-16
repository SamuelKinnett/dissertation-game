using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

public class MapController : MonoBehaviour
{
	// The X, Y and Z dimensions of the map respectively
	public int mapWidth;
	public int mapHeight;
	public int mapLength;

	// The X, Y and Z dimensions of the chunks
	public int chunkWidth;
	public int chunkHeight;
	public int chunkLength;

	// The map chunk prefab
	public GameObject mapChunkPrefab;

	// This array stores the data of the map geometry, with each element representing one 'block'.
	// Current possible values are:
	// 0 - Empty space
	// 1 - Solid
	// Could eventually add different terrain types here, even if only for graphical effect.
	private byte[,,] mapData;

	private List<MapChunkController> mapChunks;

	public byte GetBlock(int x, int y, int z)
	{
		if (x >= mapWidth ||
		    x < 0 ||
		    y >= mapHeight ||
		    y < 0 ||
		    z >= mapLength ||
		    z < 0) {
			return 1;
		}

		return mapData[x, y, z];
	}

	public void SetBlock(int x, int y, int z, byte block)
	{
		x = Mathf.Clamp(x, 0, mapWidth);
		y = Mathf.Clamp(y, 0, mapHeight);
		z = Mathf.Clamp(z, 0, mapLength);

		mapData[x, y, z] = block;

		// Find the modified chunk and update it's changed flag
		mapChunks.Single((mc) => mc.Contains(x, y, z)).ChunkUpdated = true;
	}

	// Use this for initialization
	private void Start()
	{
		mapData = new byte[mapWidth, mapHeight, mapLength];
		mapChunks = new List<MapChunkController>();

		// Add a testing plane to the map
		//for (int x = 0; x < mapWidth; ++x) {
		//	for (int z = 0; z < mapLength; ++z) {
		//		mapData[x, 0, z] = 1;
		//	}
		//}

		GenerateWorld();
		InstantiateChunks();
		GenerateMesh();
	}

	private void InstantiateChunks()
	{
		int mapWidthInChunks = (int)Math.Ceiling((double)mapWidth / (double)chunkWidth);
		int mapHeightInChunks = (int)Math.Ceiling((double)mapHeight / (double)chunkHeight);
		int mapLengthInChunks = (int)Math.Ceiling((double)mapLength / (double)chunkLength);

		Debug.Log("Instantiating Chunks (" + mapWidthInChunks + ", " + mapHeightInChunks + ", " + mapLengthInChunks + ")");

		for (int x = 0; x < mapWidthInChunks; ++x) {
			for (int y = 0; y < mapHeightInChunks; ++y) {
				for (int z = 0; z < mapLengthInChunks; ++z) {
					// Instantiate a map chunk
					var newChunk = Instantiate(mapChunkPrefab);
					newChunk.transform.parent = this.transform;

					// Initialise the chunk
					var newChunkController = newChunk.GetComponent<MapChunkController>();
					newChunkController.Initialise(x * chunkWidth, y * chunkHeight, z * chunkLength, chunkWidth, chunkHeight, chunkLength);

					// Add the chunk to the list
					mapChunks.Add(newChunkController);
				}
			}
		}

		Debug.Log("Chunks Instantiated");
	}

	private void GenerateMesh()
	{
		foreach (var currentChunk in mapChunks) {
			currentChunk.GenerateMesh();
		}
	}

	#region testing

	/// <summary>
	/// This method repeatedly uses cellular automata with decreasing probability to
	/// generate land in order to build the terrain.
	/// </summary>
	private void GenerateWorld()
	{
		// int[,] heightMap = new int[worldWidth, worldHeight]; //this array stores the height of each chunk. 0 = bedrock layer, 1 - 8 = ocean, 9 - 31 = land
		byte[,] temporaryMap = new byte[mapWidth, mapLength]; //this array is used in the generation process to create the layer before it is applied to the heightmap

		int neighbourCount = 0; //this is used to store the number of 'alive' tiles surrounding a cell;

		System.Random rand = new System.Random();

		//fileManager.CreateWorldFolder(worldName);
		double landChance = 0.78;

		//fill the heightmap with bedrock (lowest layer)
		for (int x = 0; x < mapWidth; ++x) {
			for (int z = 0; z < mapLength; ++z) {
				mapData[x, 0, z] = 1;
			}
		}

		for (int y = 1; y < mapHeight - 1; ++y) { //for each height level
			//clear the arrays of their previous contents
			Array.Clear(temporaryMap, 0, temporaryMap.Length);

			//firstly, fill most of the available area with random values (0 = empty space, 1 = filled).
			for (int tempZ = 2; tempZ < mapLength - 2; ++tempZ) {
				for (int tempX = 2; tempX < mapWidth - 2; ++tempX) {
					if (rand.NextDouble() < landChance && GetBlock(tempX, y - 1, tempZ) > 0) {
						temporaryMap[tempX, tempZ] = 1;
					}
				}
			}

			//then, simulate a 4-5 rule cellular automata 7 times
			//for the first 4 times, also fill in areas with 0 neighbours

			for (int generation = 0; generation < 5; generation++) {
				for (int tempZ = 0; tempZ < mapLength; tempZ++) {
					for (int tempX = 0; tempX < mapWidth; ++tempX) {

						neighbourCount = GetNeighbourCount(tempX, tempZ, temporaryMap);

						if (neighbourCount >= 5 || neighbourCount == 0) {
							//testing: only fill if less than the random number
							if (rand.NextDouble() < landChance && GetBlock(tempX, y - 1, tempZ) > 0) {
								temporaryMap[tempX, tempZ] = 1;
							}
						} else {
							temporaryMap[tempX, tempZ] = 0;
						}

					}
				}
			}

			//for the last three times, only use the 4-5 rule

			for (int generation = 0; generation < 4; generation++) {
				for (int tempZ = 0; tempZ < mapLength; tempZ++) {
					for (int tempX = 0; tempX < mapWidth; tempX++) {

						neighbourCount = GetNeighbourCount(tempX, tempZ, temporaryMap);

						if (neighbourCount >= 5) {
							temporaryMap[tempX, tempZ] = 1;
						} else {
							temporaryMap[tempX, tempZ] = 0;
						}

					}
				}
			}

			landChance -= 0.01; //make the next level more sparse

			//apply the layer to the heightmap

			for (int tempZ = 0; tempZ < mapLength; tempZ++) {
				for (int tempX = 0; tempX < mapWidth; tempX++) {
					if (temporaryMap[tempX, tempZ] == 1) {
						mapData[tempX, y, tempZ] = 1;
					}
				}
			}

		}
	}

	private int GetNeighbourCount(int x, int z, byte[,] temporaryMap)
	{
		int neighbourCount = 0;

		if (z != 0 && z != (mapLength - 1) && x != 0 && x != (mapWidth - 1)) { //normal comparisons of non-border tiles
			if (temporaryMap[x, z] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x + 1, z + 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x + 1, z] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x + 1, z - 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x, z - 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x - 1, z - 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x - 1, z] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x - 1, z + 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x, z + 1] == 1) {
				neighbourCount++;
			}
		} else if (z == 0) {
			if (x == 0) { //top left corner
				if (temporaryMap[x, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z + 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z + 1] == 1) {
					neighbourCount++;
				}
			} else if (x == mapWidth - 1) { //top right corner
				if (temporaryMap[x, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z + 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z + 1] == 1) {
					neighbourCount++;
				}
			} else { //anywhere along the top border
				if (temporaryMap[x, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z + 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z + 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z + 1] == 1) {
					neighbourCount++;
				}
			}
		} else if (x == 0) {
			if (z == mapLength - 1) { //bottom left corner
				if (temporaryMap[x, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z - 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z - 1] == 1) {
					neighbourCount++;
				}
			} else { //anywhere along the left border.
				if (temporaryMap[x, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z - 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z + 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z + 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z - 1] == 1) {
					neighbourCount++;
				}
			}
		} else if (z == mapLength - 1) {
			if (x == mapWidth - 1) { //bottom right corner
				if (temporaryMap[x, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z - 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z - 1] == 1) {
					neighbourCount++;
				}
			} else { //anywhere along the bottom border
				if (temporaryMap[x, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x, z - 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x - 1, z - 1] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z] == 1) {
					neighbourCount++;
				}
				if (temporaryMap[x + 1, z - 1] == 1) {
					neighbourCount++;
				}
			}
		} else if (x == mapWidth - 1) { //anywhere along the right border
			if (temporaryMap[x, z] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x, z + 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x, z - 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x - 1, z] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x - 1, z + 1] == 1) {
				neighbourCount++;
			}
			if (temporaryMap[x - 1, z - 1] == 1) {
				neighbourCount++;
			}
		}

		return neighbourCount;
	}

	#endregion
}
