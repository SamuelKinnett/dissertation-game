using Assets.Scripts.Environment.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BorderTerrainController : MonoBehaviour
{
    // The X and Y dimensions of the terrain in multiples of the map size.
    // Note that these must be of size 2^n + 1
    public int TerrainWidth;
    public int TerrainDepth;

    // The height clamps for the terrain
    public float MaxHeight;
    public float MinHeight;

    // How much the range of heights changes with every step
    public float RangeChange;

    public Vector2 textureMapDimensions;
    public MeshFilter MeshFilter;

    private Mesh mesh;
    private float[,] heightMap;

    private List<Vector3> newVertices;
    private List<int> newTriangles;
    private List<Vector2> newUV;
    private float textureUnit = 0.25f;
    private int faceCount;

    /// <summary>
    /// Generates some simple terrain using the diamonds and squares algorithm.
    /// 
    /// Developed from the pseudocode and implementation found here:
    /// http://jmecom.github.io/blog/2015/diamond-square/ 
    /// </summary>
    public void GenerateTerrain(int chunkWidth, int chunkHeight, int mapHeight)
    {
        heightMap = new float[TerrainWidth, TerrainDepth];

        // Fill in the corner values
        heightMap[0, 0] = Random.Range(MinHeight, MaxHeight);
        heightMap[TerrainWidth - 1, 0] = Random.Range(MinHeight, MaxHeight);
        heightMap[0, TerrainDepth - 1] = Random.Range(MinHeight, MaxHeight);
        heightMap[TerrainWidth - 1, TerrainDepth - 1] = Random.Range(MinHeight, MaxHeight);

        var stepSize = TerrainWidth - 1;
        var range = Mathf.Lerp(MinHeight, MaxHeight, 0.5f);

        while (stepSize > 1)
        {
            Debug.Log($"Step size: {stepSize}");
            for (int x = 0; x < TerrainWidth - 1; x += stepSize)
            {
                for (int y = 0; y < TerrainDepth - 1; y += stepSize)
                {
                    DiamondStep(x, y, stepSize, range);
                }
            }

            // We use this to stagger the rows
            var evenRow = true;

            for (int x = 0; x < TerrainWidth; x += stepSize / 2)
            {
                var posY = evenRow ? 0 : stepSize / 2;

                for (int y = posY; y < TerrainDepth; y += stepSize / 2)
                {
                    SquareStep(x, y, stepSize, range);
                }
                evenRow = !evenRow;
            }

            stepSize = stepSize / 2;
            range = Mathf.Clamp(range - RangeChange, 0, MaxHeight - MinHeight);
        }

        GenerateMesh(chunkWidth, chunkHeight, mapHeight);
    }

    private void Awake()
    {
        mesh = MeshFilter.mesh;

        newVertices = new List<Vector3>();
        newTriangles = new List<int>();
        newUV = new List<Vector2>();
    }

    /// <summary>
    /// Perform the diamond step for a given square
    /// </summary>
    /// <param name="x">The x position of the top left of the square</param>
    /// <param name="y">The y position of the top left of the square</param>
    /// <param name="stepSize">The step size</param>
    /// <param name="range">The maximum possible height adjustment</param>
    private void DiamondStep(int x, int y, int stepSize, float range)
    {
        var average =
            (heightMap[x, y] +
            heightMap[x + stepSize, y] +
            heightMap[x, y + stepSize] +
            heightMap[x + stepSize, y + stepSize]) / 4.0f;

        heightMap[x + stepSize / 2, y + stepSize / 2] = average + Random.Range(-range, range);
    }

    /// <summary>
    /// Perform the square step for a given diamond
    /// </summary>
    /// <param name="x">The x position of the centre of the diamond</param>
    /// <param name="y">The y position of the centre of the diamond</param>
    /// <param name="stepSize">The step size</param>
    /// <param name="range">The maximum possible height adjustment</param>
    private void SquareStep(int x, int y, int stepSize, float range)
    {
        var tempStepSize = stepSize / 2;

        var average =
            ((x - tempStepSize < 0 ? 0 : heightMap[x - tempStepSize, y]) +
            (x + tempStepSize >= TerrainWidth ? 0 : heightMap[x + tempStepSize, y]) +
            (y - tempStepSize < 0 ? 0 : heightMap[x, y - tempStepSize]) +
            (y + tempStepSize >= TerrainDepth ? 0 : heightMap[x, y + tempStepSize])) / 4.0f;

        heightMap[x, y] = average + Random.Range(-range, range);
    }

    private void GenerateMesh(int chunkWidth, int chunkHeight, int mapHeight)
    {
        for (int x = 0; x < TerrainWidth - 1; ++x)
        {
            for (int y = 0; y < TerrainDepth - 1; ++y)
            {
                if (((float)x + 1 == Mathf.Floor(TerrainWidth / 2.0f) || (float)x + 1 == Mathf.Ceil(TerrainWidth / 2.0f)) &&
                    ((float)y + 1 == Mathf.Floor(TerrainDepth / 2.0f) || (float)y + 1 == Mathf.Ceil(TerrainWidth / 2.0f)))
                {
                    // This is one of the tiles bordering the map, so adjust the heightmap
                    heightMap[x, y] = mapHeight;
                    heightMap[x + 1, y] = mapHeight;
                    heightMap[x, y + 1] = mapHeight;
                    heightMap[x + 1, y + 1] = mapHeight;
                }
            }
        }

        for (int x = 0; x < TerrainWidth - 1; ++x)
        {
            for (int y = 0; y < TerrainDepth - 1; ++y)
            {
                // Don't render the four tiles in the centre, as these will be replaced with the map
                if (!(((float)x + 1 == Mathf.Floor(TerrainWidth / 2.0f) || (float)x + 1 == Mathf.Ceil(TerrainWidth / 2.0f)) &&
                    ((float)y + 1 == Mathf.Floor(TerrainDepth / 2.0f) || (float)y + 1 == Mathf.Ceil(TerrainWidth / 2.0f))))
                {
                    // Reposition the terrain such that the map will sit in the centre
                    var posX = (x - (TerrainWidth / 2) + 1) * chunkWidth;
                    var posY = (y - (TerrainDepth / 2) + 1) * chunkHeight;

                    // Add the new vertices
                    newVertices.Add(new Vector3(posX, heightMap[x, y], posY));
                    newVertices.Add(new Vector3(posX, heightMap[x, y + 1], posY + chunkHeight));
                    newVertices.Add(new Vector3(posX + chunkWidth, heightMap[x + 1, y + 1], posY + chunkHeight));
                    newVertices.Add(new Vector3(posX + chunkWidth, heightMap[x + 1, y], posY));

                    ApplyTextureToFace((int)BlockType.Grass - 1);
                }
            }
        }

        // Clear the mesh and set the vertices, UVs and triangles
        mesh.Clear();
        mesh.SetVertices(newVertices);
        mesh.SetUVs(0, newUV);
        mesh.SetTriangles(newTriangles, 0);

        // Recalculate normals
        mesh.RecalculateNormals();

        // Clear the buffers
        newVertices.Clear();
        newUV.Clear();
        newTriangles.Clear();

        // Reset the face count
        faceCount = 0;
    }

    private void ApplyTextureToFace(int textureIndex)
    {
        var texturePosition = ConvertTextureIndexToTexturePosition(textureIndex);

        // Generate the triangles
        var offset = faceCount * 4;

        newTriangles.Add(offset);       // 1
        newTriangles.Add(offset + 1);   // 2
        newTriangles.Add(offset + 2);   // 3
        newTriangles.Add(offset);
        newTriangles.Add(offset + 2);
        newTriangles.Add(offset + 3);

        var xOrigin = textureUnit * texturePosition.x;
        var yOrigin = textureUnit * texturePosition.y;

        newUV.Add(new Vector2(xOrigin, yOrigin));
        newUV.Add(new Vector2(xOrigin, yOrigin + textureUnit));
        newUV.Add(new Vector2(xOrigin + textureUnit, yOrigin + textureUnit));
        newUV.Add(new Vector2(xOrigin + textureUnit, yOrigin));

        ++faceCount;
    }

    // Given a texture index, convert it to the corresponding texture
    // position. Texture indices go from left to right, top to bottom.
    private Vector2 ConvertTextureIndexToTexturePosition(int textureIndex)
    {
        var maxIndex = (int)textureMapDimensions.x * (int)textureMapDimensions.y - 1;
        var newIndex = Mathf.Clamp(textureIndex, 0, maxIndex);

        var textureX = newIndex % (int)textureMapDimensions.x;
        var textureY = (textureMapDimensions.y - 1) - (newIndex / (int)textureMapDimensions.y);

        return new Vector2(textureX, textureY);
    }
}
