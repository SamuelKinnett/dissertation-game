using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;

using Assets.Scripts.Environment.Enums;
using Assets.Scripts.Environment.Genetic_Algorithms;
using Assets.Scripts.Environment.Helpers;
using Assets.Scripts.Environment.Structs;

using GAF;
using GAF.Operators;

public class SyncListGeneTuple : SyncListStruct<GeneTuple>
{
}

public class MapController : NetworkBehaviour
{
    // The width, height and depth of the map
    [SyncVar(hook = "OnDimensionsChanged")]
    public Vector3 mapDimensions;

    // Used to reduce the number of map updates on the clients
    [SyncVar(hook = "OnUpdatingGenesChanged")]
    public bool updatingGenes;

    // The X, Y and Z dimensions of the chunks
    public int chunkWidth;
    public int chunkHeight;
    public int chunkDepth;

    // The map chunk prefab
    public GameObject mapChunkPrefab;

    // This SyncList stores the tuples that form the genotype
    private SyncListGeneTuple currentGenes = new SyncListGeneTuple();

    // This array stores the data of the map geometry, with each element representing one 'block'.
    // A 0 represents empty space, numbers above this represent different block types.
    private static byte[,,] mapData;

    private static List<MapChunkController> mapChunks;

    private static int wallHeight = 4;

    // GA related variables
    static Chromosome currentMapChromosome;

    static bool generationInProgress;
    static bool mapUpdateNeeded;

    /// <summary>
    /// Callback for when the GA has finished a generation
    /// </summary>
    public static void ga_OnGenerationComplete(object sender, GaEventArgs e)
    {
    }

    /// <summary>
    /// Callback for when the GA has finished a run
    /// </summary>
    public static void ga_OnRunComplete(object sender, GaEventArgs e)
    {
        var fittestMap = e.Population.GetTop(1).First();

        currentMapChromosome = fittestMap;

        mapUpdateNeeded = true;
        generationInProgress = false;
        Debug.Log("Run complete");
    }

    public override void OnStartClient()
    {
        currentGenes.Callback = OnCurrentGenesUpdated;
        base.OnStartClient();
    }

    public byte GetBlock(int x, int y, int z)
    {
        if (x >= mapDimensions.x ||
            x < 0 ||
            y >= mapDimensions.y ||
            y < 0 ||
            z >= mapDimensions.z ||
            z < 0)
        {
            return 1;
        }

        return mapData[x, y, z];
    }

    public void SetBlock(int x, int y, int z, BlockType block, bool updateChunk = true)
    {
        x = Mathf.Clamp(x, 0, (int)mapDimensions.x - 1);
        y = Mathf.Clamp(y, 0, (int)mapDimensions.y - 1);
        z = Mathf.Clamp(z, 0, (int)mapDimensions.z - 1);

        if (mapData[x, y, z] != (byte)block)
        {
            mapData[x, y, z] = (byte)block;

            if (updateChunk)
            {
                // Find the modified chunk and update it's changed flag
                mapChunks.Single((mc) => mc.Contains(x, y, z)).ChunkUpdated = true;
            }
        }
    }

    public void SetLargeBlock(int x, int y, int z, BlockType block)
    {
        var tempX = x * 2;
        var tempY = y * 2;
        var tempZ = z * 2;

        SetBlock(tempX, tempY, tempZ, block, false);
        SetBlock(tempX + 1, tempY, tempZ, block, false);
        SetBlock(tempX, tempY + 1, tempZ, block, false);
        SetBlock(tempX + 1, tempY + 1, tempZ, block, false);
        SetBlock(tempX, tempY, tempZ + 1, block, false);
        SetBlock(tempX + 1, tempY, tempZ + 1, block, false);
        SetBlock(tempX, tempY + 1, tempZ + 1, block, false);
        SetBlock(tempX + 1, tempY + 1, tempZ + 1, block, false);
    }

    private void UpdateMapWithCurrentGenes()
    {
        currentMapChromosome = new Chromosome(currentGenes.Select((geneTuple) => new Gene(geneTuple)));
        Debug.Log("Updated current chromosome");

        UpdateMapWithMapSketch(MapSketchHelpers.ConvertChromosomeToMapSketch(currentMapChromosome, (int)mapDimensions.x / 2, (int)mapDimensions.z / 2));
        Debug.Log("Updated map");
    }

    public void UpdateMapWithMapSketch(TileType[,] mapSketch)
    {
        var mapSketchWidth = (int)mapDimensions.x / 2;
        var mapSketchHeight = (int)mapDimensions.z / 2;

        for (int curX = 0; curX < mapSketchWidth; ++curX)
        {
            for (int curY = 0; curY < mapSketchHeight; ++curY)
            {
                switch (mapSketch[curX, curY])
                {
                    case TileType.Impassable:
                        for (int i = 0; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, BlockType.Wall);
                        }
                        break;

                    case TileType.Passable:
                        SetLargeBlock(curX, 0, curY, BlockType.Floor);
                        for (int i = 1; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 0);
                        }
                        break;

                    case TileType.Team1Spawn:
                        SetLargeBlock(curX, 0, curY, BlockType.Team1Spawn);
                        for (int i = 1; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 0);
                        }
                        // TODO: Set spawn point here
                        break;

                    case TileType.Team2Spawn:
                        SetLargeBlock(curX, 0, curY, BlockType.Team2Spawn);
                        for (int i = 1; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 0);
                        }
                        // TODO: Set spawn point here
                        break;

                    case TileType.Barrier:
                        SetLargeBlock(curX, 0, curY, BlockType.Wall);
                        SetLargeBlock(curX, 1, curY, BlockType.Wall);
                        for (int i = 2; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 0);
                        }
                        break;

                    case TileType.CapturePoint:
                        SetLargeBlock(curX, 0, curY, BlockType.CapturePoint);
                        for (int i = 1; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 0);
                        }
                        break;

                    default:
                        for (int i = 0; i < 2; ++i)
                        {
                            SetLargeBlock(curX, i, curY, BlockType.Wall);
                        }
                        break;
                }
            }
        }

        GenerateMesh();
    }

    // Use this for initialization
    private void Start()
    {
        updatingGenes = true;

        mapData = new byte[(int)mapDimensions.x, (int)mapDimensions.y, (int)mapDimensions.z];
        mapChunks = new List<MapChunkController>();
        InstantiateChunks();

        mapUpdateNeeded = false;

        if (isServer)
        {
            GeneticAlgorithmHelpers.mapSketchWidth = (int)mapDimensions.x / 2;
            GeneticAlgorithmHelpers.mapSketchHeight = (int)mapDimensions.z / 2;

            // Create the default genes
            for (int i = 0; i < 45; ++i)
            {
                currentGenes.Add(new GeneTuple(0, 0, 0));
            }

            var horizontalCorridorLength = (int)mapDimensions.x / 4;
            var verticalCorridorLength = (int)mapDimensions.z / 4;

            // Add the default centre arena
            currentGenes[0] = new GeneTuple(horizontalCorridorLength / 2, verticalCorridorLength / 2, horizontalCorridorLength);

            // Add the default corridors
            currentGenes[15] = new GeneTuple(4, 4, horizontalCorridorLength / 2);
            currentGenes[16] = new GeneTuple(4, 4, -verticalCorridorLength / 2);
            currentGenes[17] = new GeneTuple(4, 4 + verticalCorridorLength / 2, horizontalCorridorLength / 2);
            currentGenes[18] = new GeneTuple(4 + horizontalCorridorLength / 2, 4, -verticalCorridorLength / 2);
            currentGenes[19] = new GeneTuple(((int)mapDimensions.x / 2) - 5 - horizontalCorridorLength / 2, ((int)mapDimensions.z / 2) - 5, horizontalCorridorLength / 2);
            currentGenes[20] = new GeneTuple(((int)mapDimensions.x / 2) - 5, ((int)mapDimensions.z / 2) - 5 - verticalCorridorLength / 2, -verticalCorridorLength / 2);
            currentGenes[21] = new GeneTuple(((int)mapDimensions.x / 2) - 5 - horizontalCorridorLength / 2, ((int)mapDimensions.z / 2) - 5 - verticalCorridorLength / 2, horizontalCorridorLength / 2);
            currentGenes[22] = new GeneTuple(((int)mapDimensions.x / 2) - 5 - horizontalCorridorLength / 2, ((int)mapDimensions.z / 2) - 5 - verticalCorridorLength / 2, -verticalCorridorLength / 2);

            // Add the default capture point
            currentGenes[44] = new GeneTuple((int)mapDimensions.x / 4 - 1, (int)mapDimensions.z / 4 - 1, 2);
            updatingGenes = false;

            currentMapChromosome = new Chromosome(currentGenes.Select((geneTuple) => new Gene(geneTuple)));
            mapUpdateNeeded = true;
        }
    }

    private void Update()
    {
        if (isServer && mapUpdateNeeded)
        {
            // Update the current gene list for network synchronisation
            for (int i = 0; i < currentMapChromosome.Genes.Count; ++i)
            {
                var geneValue = (GeneTuple)currentMapChromosome.Genes[i].ObjectValue;

                if (currentGenes[i] != geneValue)
                {
                    currentGenes[i] = geneValue;
                }
            }

            mapUpdateNeeded = false;

            Invoke("GenerateWorld", 15);
        }
    }

    private void InstantiateChunks()
    {
        int mapWidthInChunks = (int)System.Math.Ceiling(mapDimensions.x / (double)chunkWidth);
        int mapHeightInChunks = (int)System.Math.Ceiling(mapDimensions.y / (double)chunkHeight);
        int mapLengthInChunks = (int)System.Math.Ceiling(mapDimensions.z / (double)chunkDepth);

        Debug.Log("Instantiating Chunks (" + mapWidthInChunks + ", " + mapHeightInChunks + ", " + mapLengthInChunks + ")");

        for (int x = 0; x < mapWidthInChunks; ++x)
        {
            for (int y = 0; y < mapHeightInChunks; ++y)
            {
                for (int z = 0; z < mapLengthInChunks; ++z)
                {
                    // Instantiate a map chunk
                    var newChunk = Instantiate(mapChunkPrefab);
                    newChunk.transform.parent = this.transform;

                    // Initialise the chunk
                    var newChunkController = newChunk.GetComponent<MapChunkController>();
                    newChunkController.Initialise(x * chunkWidth, y * chunkHeight, z * chunkDepth, chunkWidth, chunkHeight, chunkDepth);

                    // Add the chunk to the list
                    mapChunks.Add(newChunkController);
                }
            }
        }

        Debug.Log("Chunks Instantiated");
    }

    private void GenerateMesh()
    {
        foreach (var currentChunk in mapChunks)
        {
            currentChunk.GenerateMesh();
        }
    }

    private void GenerateWorld()
    {
        generationInProgress = true;

        var population = new Population();

        // Add one hundred copies of the current chromosome
        for (int i = 0; i < 100; ++i)
        {
            population.Solutions.Add(currentMapChromosome.DeepClone(true));
        }

        // Create the elite operator with an elitism percentage of 5 (the top
        // five percent of solutions will pass through to the next generation
        // without being modified).
        var elite = new Elite(5);

        // Create the crossover operator. The crossover probability is 80% and
        // a double point crossover type is used. This involves two points in
        // each chromosome being selected to identify a 'middle section', which
        // is then swapped between the two parents.
        var crossover = new Crossover(0.8) { CrossoverType = CrossoverType.DoublePoint };

        // Create the mutation operator.
        var mutation = new MapMutate(0.04, (int)mapDimensions.x / 2, (int)mapDimensions.z / 2);

        // Create the genetic algorithm and assign callbacks
        var geneticAlgorithm = new GeneticAlgorithm(population, GeneticAlgorithmHelpers.FitnessFunction);
        geneticAlgorithm.OnGenerationComplete += ga_OnGenerationComplete;
        geneticAlgorithm.OnRunComplete += ga_OnRunComplete;

        // Add the operators to the genetic algorithm process pipeline
        geneticAlgorithm.Operators.Add(elite);
        geneticAlgorithm.Operators.Add(crossover);
        geneticAlgorithm.Operators.Add(mutation);

        // Run the genetic algorithm
        geneticAlgorithm.RunAsync(GeneticAlgorithmHelpers.Terminate);
    }

    #region Callbacks

    private void OnDimensionsChanged(Vector3 newDimensions)
    {
        Debug.Log("Map dimensions changed");
        mapChunks.Clear();
        InstantiateChunks();
    }

    private void OnUpdatingGenesChanged(bool newValue)
    {
        if (newValue == false)
        {
            UpdateMapWithCurrentGenes();
        }
    }

    private void OnCurrentGenesUpdated(SyncListStruct<GeneTuple>.Operation operation, int index)
    {
        if (!updatingGenes)
        {
            UpdateMapWithCurrentGenes();
        }
    }

    #endregion
}
