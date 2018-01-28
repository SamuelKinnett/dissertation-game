using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapController : MonoBehaviour
{
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] MeshCollider meshCollider;

	// The X, Y and Z dimensions of the map respectively
	public int mapWidth;
	public int mapLength;
	public int mapHeight;

	// This array stores the data of the map geometry, with each element representing one 'block'.
	// Current possible values are:
	// 0 - Empty space
	// 1 - Solid
	// Could eventually add different terrain types here, even if only for graphical effect.
	private byte[,,] mapData;

	private Mesh mesh;

	private List<Vector3> newVertices;
	private List<int> newTriangles;
	private List<Vector2> newUV;

	private float textureUnit = 0.25f;
	private Vector2 textureStone = new Vector2(1, 0);
	private Vector2 textureGrass = new Vector2(0, 1);

	private int faceCount;

	// Use this for initialization
	private void Start()
	{
		mapData = new byte[mapWidth, mapHeight, mapLength];

		// Add a testing plane to the map
		for (int x = 0; x < mapWidth; ++x) {
			for (int z = 0; z < mapLength; ++z) {
				mapData[x, 0, z] = 1;
			}
		}

		mesh = meshFilter.mesh;

		newVertices = new List<Vector3>();
		newTriangles = new List<int>();
		newUV = new List<Vector2>();

		GenerateMesh();
	}
	
	// Update is called once per frame
	private void Update()
	{
		
	}

	private void UpdateMesh()
	{
		// Clear the mesh and set the vertices, UVs and triangles
		mesh.Clear();
		mesh.SetVertices(newVertices);
		mesh.SetUVs(0, newUV);
		mesh.SetTriangles(newTriangles, 0);

		// Optimise the mesh
		MeshUtility.Optimize(mesh);

		// Recalculate normals
		mesh.RecalculateNormals();

		// Create the collision mesh
		meshCollider.sharedMesh = mesh;

		// Clear the buffers
		newVertices.Clear();
		newUV.Clear();
		newTriangles.Clear();

		// Reset the face count
		faceCount = 0;
	}

	private void GenerateMesh()
	{
		for (int x = 0; x < mapWidth; ++x) {
			for (int y = 0; y < mapHeight; ++y) {
				for (int z = 0; z < mapLength; ++z) {
					byte curBlock = GetBlock(x, y, z);

					// If this block is solid
					if (curBlock > 0) {
						// Compare the block to all of its neighbours. Any side that faces air should
						// have a face rendered.

						if (GetBlock(x, y + 1, z) == 0) {
							CreateCubeTopFace(x, y, z, curBlock);
						}

						if (GetBlock(x, y, z + 1) == 0) {
							CreateCubeNorthFace(x, y, z, curBlock);
						}

						if (GetBlock(x + 1, y, z) == 0) {
							CreateCubeEastFace(x, y, z, curBlock);
						}

						if (GetBlock(x, y, z - 1) == 0) {
							CreateCubeSouthFace(x, y, z, curBlock);
						}

						if (GetBlock(x - 1, y, z) == 0) {
							CreateCubeWestFace(x, y, z, curBlock);
						}

						if (GetBlock(x, y - 1, z) == 0) {
							CreateCubeBottomFace(x, y, z, curBlock);
						}
					}
				}
			}
		}

		UpdateMesh();
	}

	private byte GetBlock(int x, int y, int z)
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

	private void CreateCubeTopFace(int x, int y, int z, byte block)
	{
		// Add the new vertices
		newVertices.Add(new Vector3(x, y, z));
		newVertices.Add(new Vector3(x, y, z + 1));
		newVertices.Add(new Vector3(x + 1, y, z + 1));
		newVertices.Add(new Vector3(x + 1, y, z));

		ApplyTextureToFace(textureStone);
	}

	private void CreateCubeNorthFace(int x, int y, int z, byte block)
	{
		// Add the new vertices
		newVertices.Add(new Vector3(x + 1, y - 1, z + 1));
		newVertices.Add(new Vector3(x + 1, y, z + 1));
		newVertices.Add(new Vector3(x, y, z + 1));
		newVertices.Add(new Vector3(x, y - 1, z + 1));

		ApplyTextureToFace(textureStone);
	}

	private void CreateCubeEastFace(int x, int y, int z, byte block)
	{
		// Add the new vertices
		newVertices.Add(new Vector3(x + 1, y - 1, z));
		newVertices.Add(new Vector3(x + 1, y, z));
		newVertices.Add(new Vector3(x + 1, y, z + 1));
		newVertices.Add(new Vector3(x + 1, y - 1, z + 1));

		ApplyTextureToFace(textureStone);
	}

	private void CreateCubeSouthFace(int x, int y, int z, byte block)
	{
		// Add the new vertices
		newVertices.Add(new Vector3(x, y - 1, z));
		newVertices.Add(new Vector3(x, y, z));
		newVertices.Add(new Vector3(x + 1, y, z));
		newVertices.Add(new Vector3(x + 1, y - 1, z));

		ApplyTextureToFace(textureStone);
	}

	private void CreateCubeWestFace(int x, int y, int z, byte block)
	{
		// Add the new vertices
		newVertices.Add(new Vector3(x, y - 1, z + 1));
		newVertices.Add(new Vector3(x, y, z + 1));
		newVertices.Add(new Vector3(x, y, z));
		newVertices.Add(new Vector3(x, y - 1, z));

		ApplyTextureToFace(textureStone);
	}

	private void CreateCubeBottomFace(int x, int y, int z, byte block)
	{
		// Add the new vertices
		newVertices.Add(new Vector3(x, y - 1, z));
		newVertices.Add(new Vector3(x + 1, y - 1, z));
		newVertices.Add(new Vector3(x + 1, y - 1, z + 1));
		newVertices.Add(new Vector3(x, y - 1, z + 1));

		ApplyTextureToFace(textureStone);
	}

	private void ApplyTextureToFace(Vector2 texturePosition)
	{
		// Generate the triangles
		var offset = faceCount * 4;

		newTriangles.Add(offset);		// 1
		newTriangles.Add(offset + 1); 	// 2
		newTriangles.Add(offset + 2);	// 3
		newTriangles.Add(offset);
		newTriangles.Add(offset + 2);
		newTriangles.Add(offset + 3);

		var xOrigin = textureUnit * texturePosition.x;
		var yOrigin = textureUnit * texturePosition.y;

		newUV.Add(new Vector2(xOrigin, yOrigin));
		newUV.Add(new Vector2(xOrigin + textureUnit, yOrigin));
		newUV.Add(new Vector2(xOrigin + textureUnit, yOrigin + textureUnit));
		newUV.Add(new Vector2(xOrigin, yOrigin + textureUnit));

		++faceCount;
	}
}
