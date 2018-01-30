using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MapChunkController : MonoBehaviour
{
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] MeshCollider meshCollider;

	private Mesh mesh;

	private List<Vector3> newVertices;
	private List<int> newTriangles;
	private List<Vector2> newUV;

	private float textureUnit = 0.25f;
	private Vector2 textureStone = new Vector2(1, 0);
	private Vector2 textureGrass = new Vector2(0, 1);

	private int faceCount;

	private int chunkX;
	private int chunkY;
	private int chunkZ;
	private int chunkWidth;
	private int chunkHeight;
	private int chunkLength;

	private bool initialised;
	private MapController mapController;

	public void Initialise(
		int chunkX,
		int chunkY,
		int chunkZ,
		int chunkWidth,
		int chunkHeight,
		int chunkLength)
	{
		this.chunkX = chunkX;
		this.chunkY = chunkY;
		this.chunkZ = chunkZ;
		this.chunkWidth = chunkWidth;
		this.chunkHeight = chunkHeight;
		this.chunkLength = chunkLength;

		mapController = this.GetComponentInParent<MapController>();

		initialised = true;
	}

	public void GenerateMesh()
	{
		for (int x = chunkX; x < chunkX + chunkWidth; ++x) {
			for (int y = chunkY; y < chunkY + chunkHeight; ++y) {
				for (int z = chunkZ; z < chunkZ + chunkLength; ++z) {
					byte curBlock = mapController.GetBlock(x, y, z);

					// If this block is solid
					if (curBlock > 0) {
						// Compare the block to all of its neighbours. Any side that faces air should
						// have a face rendered.

						if (mapController.GetBlock(x, y + 1, z) == 0) {
							CreateCubeTopFace(x, y, z, curBlock);
						}

						if (mapController.GetBlock(x, y, z + 1) == 0) {
							CreateCubeNorthFace(x, y, z, curBlock);
						}

						if (mapController.GetBlock(x + 1, y, z) == 0) {
							CreateCubeEastFace(x, y, z, curBlock);
						}

						if (mapController.GetBlock(x, y, z - 1) == 0) {
							CreateCubeSouthFace(x, y, z, curBlock);
						}

						if (mapController.GetBlock(x - 1, y, z) == 0) {
							CreateCubeWestFace(x, y, z, curBlock);
						}

						if (mapController.GetBlock(x, y - 1, z) == 0) {
							CreateCubeBottomFace(x, y, z, curBlock);
						}
					}
				}
			}
		}

		UpdateMesh();
	}

	private void Awake()
	{
		initialised = false;

		mesh = meshFilter.mesh;

		newVertices = new List<Vector3>();
		newTriangles = new List<int>();
		newUV = new List<Vector2>();
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
