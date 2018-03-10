using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

using UnityEngine;
using UnityEditor;

using GAF;
using GAF.Extensions;
using GAF.Operators;
using Assets.Scripts.Environment.Enums;
using Assets.Scripts.Environment.Genetic_Algorithms;
using UnityEngine.Networking;

public class MapController : NetworkBehaviour
{
    // The X, Y and Z dimensions of the map respectively
    public static int mapWidth;
    public static int mapHeight;
    public static int mapLength;

    // Used to set the map size in the insepctor
    public int setMapWidth;
    public int setMapHeight;
    public int setMapLength;

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
    private static byte[,,] mapData;

    private static List<MapChunkController> mapChunks;

    private static int wallHeight = 3;

    // GA related variables
    static Chromosome currentMapChromosome;

    static bool generationInProgress;
    static bool mapUpdateNeeded;

    public byte GetBlock(int x, int y, int z)
    {
        if (x >= mapWidth ||
            x < 0 ||
            y >= mapHeight ||
            y < 0 ||
            z >= mapLength ||
            z < 0)
        {
            return 1;
        }

        return mapData[x, y, z];
    }

    public static void SetBlock(int x, int y, int z, byte block)
    {
        x = Mathf.Clamp(x, 0, mapWidth - 1);
        y = Mathf.Clamp(y, 0, mapHeight - 1);
        z = Mathf.Clamp(z, 0, mapLength - 1);

        mapData[x, y, z] = block;

        // Find the modified chunk and update it's changed flag
        mapChunks.Single((mc) => mc.Contains(x, y, z)).ChunkUpdated = true;
    }

    public static void UpdateMapWithMapSketch(TileType[,] mapSketch)
    {
        for (int curX = 0; curX < mapWidth / 2; ++curX)
        {
            for (int curY = 0; curY < mapLength / 2; ++curY)
            {
                switch (mapSketch[curX, curY])
                {
                    case TileType.Impassable:
                        for (int i = 0; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 1);
                        }
                        break;

                    case TileType.Passable:
                        SetLargeBlock(curX, 0, curY, 1);
                        break;

                    case TileType.Team1Spawn:
                        SetLargeBlock(curX, 0, curY, 1);
                        // TODO: Set spawn point here
                        break;

                    case TileType.Team2Spawn:
                        SetLargeBlock(curX, 0, curY, 1);
                        // TODO: Set spawn point here
                        break;

                    default:
                        for (int i = 0; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 1);
                        }
                        break;
                }
            }
        }
    }

    private static void SetLargeBlock(int x, int y, int z, byte block)
    {
        var tempX = x * 2;
        var tempY = y * 2;
        var tempZ = z * 2;

        SetBlock(tempX, tempY, tempZ, block);
        SetBlock(tempX + 1, tempY, tempZ, block);
        SetBlock(tempX, tempY + 1, tempZ, block);
        SetBlock(tempX + 1, tempY + 1, tempZ, block);
        SetBlock(tempX, tempY, tempZ + 1, block);
        SetBlock(tempX + 1, tempY, tempZ + 1, block);
        SetBlock(tempX, tempY + 1, tempZ + 1, block);
        SetBlock(tempX + 1, tempY + 1, tempZ + 1, block);
    }

    // Use this for initialization
    private void Start()
    {
        mapWidth = setMapWidth;
        mapHeight = setMapHeight;
        mapLength = setMapLength;

        mapData = new byte[mapWidth, mapHeight, mapLength];
        mapChunks = new List<MapChunkController>();

        mapUpdateNeeded = false;

        // Create the default chromosome
        var defaultChromosome = new Chromosome();

        for (int i = 0; i < 45; ++i)
        {
            defaultChromosome.Add(new Gene(new Tuple<int, int, int>(0, 0, 0)));
        }

        // Add the default centre arena
        defaultChromosome.Genes[0] = new Gene(new Tuple<int, int, int>(12, 12, 8));

        // Add the default corridors
        // TODO: Make these scalable with the map rather than hard coded
        defaultChromosome.Genes[15] = new Gene(new Tuple<int, int, int>(4, 4, 16));
        defaultChromosome.Genes[16] = new Gene(new Tuple<int, int, int>(4, 4, -16));
        defaultChromosome.Genes[17] = new Gene(new Tuple<int, int, int>(12, 27, 16));
        defaultChromosome.Genes[18] = new Gene(new Tuple<int, int, int>(27, 12, -16));
        defaultChromosome.Genes[19] = new Gene(new Tuple<int, int, int>(4, 18, 10));
        defaultChromosome.Genes[20] = new Gene(new Tuple<int, int, int>(18, 4, -10));
        defaultChromosome.Genes[21] = new Gene(new Tuple<int, int, int>(13, 18, 10));
        defaultChromosome.Genes[22] = new Gene(new Tuple<int, int, int>(18, 13, -10));

        currentMapChromosome = defaultChromosome;

        // Add a testing plane to the map
        //for (int x = 0; x < mapWidth; ++x) {
        //	for (int z = 0; z < mapLength; ++z) {
        //		mapData[x, 0, z] = 1;
        //	}
        //}

        InstantiateChunks();
        GenerateMesh();
        GenerateWorld();
    }

    private void Update()
    {
        if (mapUpdateNeeded)
        {
            UpdateMapWithMapSketch(ConvertChromosomeToMapSketch(currentMapChromosome));
            GenerateMesh();
            mapUpdateNeeded = false;
        }
    }

    private void InstantiateChunks()
    {
        int mapWidthInChunks = (int)System.Math.Ceiling((double)mapWidth / (double)chunkWidth);
        int mapHeightInChunks = (int)System.Math.Ceiling((double)mapHeight / (double)chunkHeight);
        int mapLengthInChunks = (int)System.Math.Ceiling((double)mapLength / (double)chunkLength);

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
        foreach (var currentChunk in mapChunks)
        {
            currentChunk.GenerateMesh();
        }
    }

    /// <summary>
    /// Converts a chromosome into a map sketch
    /// </summary>
    public static TileType[,] ConvertChromosomeToMapSketch(Chromosome newChromosome)
    {
        // The map sketch works on a different resolution than the normal
        // grid, so we need to calculate a new width/height for this.
        var largeWidth = mapWidth / 2;
        var largeLength = mapLength / 2;

        // Create a new map sketch filled with impassable tiles
        var mapSketch = new TileType[largeWidth, largeLength];

        // Build the map
        for (int currentGeneIndex = 0; currentGeneIndex < newChromosome.Genes.Count; ++currentGeneIndex)
        {
            var geneTuple = (Tuple<int, int, int>)newChromosome.Genes[currentGeneIndex].ObjectValue;
            if (currentGeneIndex < 15)
            {
                // This gene is an arena
                int arenaX = geneTuple.Item1;
                int arenaY = geneTuple.Item2;
                int arenaSize = geneTuple.Item3;
                for (int curX = arenaX; curX < arenaX + arenaSize; ++curX)
                {
                    for (int curY = arenaY; curY < arenaY + arenaSize; ++curY)
                    {
                        if (curX >= 0 &&
                            curY >= 0 &&
                            curX < largeWidth &&
                            curY < largeLength)
                        {
                            mapSketch[curX, curY] = TileType.Passable;
                        }
                    }
                }
            }
            else if (currentGeneIndex < 40)
            {
                // This gene is a corridor or a barrier. Both are encoded in a
                // similar manner and so we'll consider them at the same time.
                int corridorX = geneTuple.Item1;
                int corridorY = geneTuple.Item2;
                int corridorLength = geneTuple.Item3;

                if (corridorLength > 1)
                {
                    // Horizontal corridor/barrier
                    for (int curX = corridorX; curX < corridorX + corridorLength; ++curX)
                    {
                        if (currentGeneIndex < 30)
                        {
                            // This is a corridor
                            // All corridors have a fixed width of three tiles
                            for (int i = -1; i < 2; ++i)
                            {
                                if (curX >= 0 &&
                                    corridorY + i >= 0 &&
                                    curX < largeWidth &&
                                    corridorY + i < largeLength)
                                {
                                    mapSketch[curX, corridorY + i] = TileType.Passable;
                                }
                            }
                        }
                        else
                        {
                            // This is a barrier
                            // All barriers are only one tile thick
                            if (curX >= 0 &&
                                corridorY >= 0 &&
                                curX < largeWidth &&
                                corridorY < largeLength)
                            {
                                mapSketch[curX, corridorY] = TileType.Barrier;
                            }
                        }
                    }
                }
                else
                {
                    // Vertical corridor/barrier
                    for (int curY = corridorY; curY < corridorY - corridorLength; ++curY)
                    {
                        if (currentGeneIndex < 30)
                        {
                            // This is a corridor
                            // All corridors have a fixed width of three tiles
                            for (int i = -1; i < 2; ++i)
                            {
                                if (curY >= 0 &&
                                    corridorX + i >= 0 &&
                                    curY < largeLength &&
                                    corridorX + i < largeWidth)
                                {
                                    mapSketch[corridorX + i, curY] = TileType.Passable;
                                }
                            }
                        }
                        else
                        {
                            // This is a barrier
                            // All barriers are only one tile thick
                            if (curY >= 0 &&
                                corridorX >= 0 &&
                                curY < largeLength &&
                                corridorX < largeWidth)
                            {
                                mapSketch[corridorX, curY] = TileType.Barrier;
                            }
                        }
                    }
                }
            }
            else if (currentGeneIndex < 44)
            {
                var pickupX = geneTuple.Item1;
                var pickupY = geneTuple.Item2;
                var pickupValue = (PickupType)geneTuple.Item3;
                // This gene is a pickup

                var mapSketchPickupValue = TileType.Passable;
                switch (pickupValue)
                {
                    case PickupType.Health:
                        mapSketchPickupValue = TileType.HealthPickup;
                        break;
                }

                mapSketch[pickupX, pickupY] = mapSketchPickupValue;
            }
            else
            {
                // This gene is the capture point
                var capturePointX = geneTuple.Item1;
                var capturePointY = geneTuple.Item2;

                mapSketch[capturePointX, capturePointY] = TileType.CapturePoint;
            }
        }

        // Add in the default spawn rooms
        for (int spawnX = 2; spawnX < 7; ++spawnX)
        {
            for (int spawnY = 2; spawnY < 7; ++spawnY)
            {
                mapSketch[spawnX, spawnY] = TileType.Team1Spawn;
            }
        }
        for (int spawnX = largeWidth - 8; spawnX < largeWidth - 2; ++spawnX)
        {
            for (int spawnY = largeLength - 8; spawnY < largeLength - 2; ++spawnY)
            {
                mapSketch[spawnX, spawnY] = TileType.Team2Spawn;
            }
        }

        return mapSketch;
    }

    #region Genetic Algorithms

    public static double FitnessFunction(Chromosome chromosome)
    {
        // TODO: convert genotype to phenotype and evaluate
        // Temporary testing value
        var mapSketch = ConvertChromosomeToMapSketch(chromosome);

        return chromosome.Genes.Average((gene) =>
        {
            var tuple = (Tuple<int, int, int>)gene.ObjectValue;
            return (tuple.Item1 > 0 && tuple.Item2 > 0 && tuple.Item3 > 0) ? 1 : 0;
        });
    }

    public static bool Terminate(Population population, int currentGeneration, long currentEvaluation)
    {
        return currentGeneration > 10;
    }

    /// <summary>
    /// Callback for when the GA has finished a generation
    /// </summary>
    public static void ga_OnGenerationComplete(object sender, GaEventArgs e)
    {
        var test = 1;
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
        // var mutation = new MapMutate(0.04);
        var mutation = new SwapMutate(0.04);

        // Create the genetic algorithm and assign callbacks
        var geneticAlgorithm = new GeneticAlgorithm(population, FitnessFunction);
        geneticAlgorithm.OnGenerationComplete += ga_OnGenerationComplete;
        geneticAlgorithm.OnRunComplete += ga_OnRunComplete;

        // Add the operators to the genetic algorithm process pipeline
        geneticAlgorithm.Operators.Add(elite);
        geneticAlgorithm.Operators.Add(crossover);
        geneticAlgorithm.Operators.Add(mutation);

        // Run the genetic algorithm
        geneticAlgorithm.Run(Terminate);
    }

    #endregion

    #region testing

    /// <summary>
    /// This method repeatedly uses cellular automata with decreasing probability to
    /// generate land in order to build the terrain.
    /// </summary>
    private void GenerateCellularAutomataWorld()
    {
        // int[,] heightMap = new int[worldWidth, worldHeight]; //this array stores the height of each chunk. 0 = bedrock layer, 1 - 8 = ocean, 9 - 31 = land
        byte[,] temporaryMap = new byte[mapWidth, mapLength]; //this array is used in the generation process to create the layer before it is applied to the heightmap

        int neighbourCount = 0; //this is used to store the number of 'alive' tiles surrounding a cell;

        System.Random rand = new System.Random();

        //fileManager.CreateWorldFolder(worldName);
        double landChance = 0.78;

        //fill the heightmap with bedrock (lowest layer)
        for (int x = 0; x < mapWidth; ++x)
        {
            for (int z = 0; z < mapLength; ++z)
            {
                mapData[x, 0, z] = 1;
            }
        }

        for (int y = 1; y < mapHeight - 1; ++y)
        { //for each height level
          //clear the arrays of their previous contents
            Array.Clear(temporaryMap, 0, temporaryMap.Length);

            //firstly, fill most of the available area with random values (0 = empty space, 1 = filled).
            for (int tempZ = 2; tempZ < mapLength - 2; ++tempZ)
            {
                for (int tempX = 2; tempX < mapWidth - 2; ++tempX)
                {
                    if (rand.NextDouble() < landChance && GetBlock(tempX, y - 1, tempZ) > 0)
                    {
                        temporaryMap[tempX, tempZ] = 1;
                    }
                }
            }

            //then, simulate a 4-5 rule cellular automata 7 times
            //for the first 4 times, also fill in areas with 0 neighbours

            for (int generation = 0; generation < 5; generation++)
            {
                for (int tempZ = 0; tempZ < mapLength; tempZ++)
                {
                    for (int tempX = 0; tempX < mapWidth; ++tempX)
                    {

                        neighbourCount = GetNeighbourCount(tempX, tempZ, temporaryMap);

                        if (neighbourCount >= 5 || neighbourCount == 0)
                        {
                            //testing: only fill if less than the random number
                            if (rand.NextDouble() < landChance && GetBlock(tempX, y - 1, tempZ) > 0)
                            {
                                temporaryMap[tempX, tempZ] = 1;
                            }
                        }
                        else
                        {
                            temporaryMap[tempX, tempZ] = 0;
                        }

                    }
                }
            }

            //for the last three times, only use the 4-5 rule

            for (int generation = 0; generation < 4; generation++)
            {
                for (int tempZ = 0; tempZ < mapLength; tempZ++)
                {
                    for (int tempX = 0; tempX < mapWidth; tempX++)
                    {

                        neighbourCount = GetNeighbourCount(tempX, tempZ, temporaryMap);

                        if (neighbourCount >= 5)
                        {
                            temporaryMap[tempX, tempZ] = 1;
                        }
                        else
                        {
                            temporaryMap[tempX, tempZ] = 0;
                        }

                    }
                }
            }

            landChance -= 0.01; //make the next level more sparse

            //apply the layer to the heightmap

            for (int tempZ = 0; tempZ < mapLength; tempZ++)
            {
                for (int tempX = 0; tempX < mapWidth; tempX++)
                {
                    if (temporaryMap[tempX, tempZ] == 1)
                    {
                        mapData[tempX, y, tempZ] = 1;
                    }
                }
            }

        }
    }

    private int GetNeighbourCount(int x, int z, byte[,] temporaryMap)
    {
        int neighbourCount = 0;

        if (z != 0 && z != (mapLength - 1) && x != 0 && x != (mapWidth - 1))
        { //normal comparisons of non-border tiles
            if (temporaryMap[x, z] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x + 1, z + 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x + 1, z] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x + 1, z - 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x, z - 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x - 1, z - 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x - 1, z] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x - 1, z + 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x, z + 1] == 1)
            {
                neighbourCount++;
            }
        }
        else if (z == 0)
        {
            if (x == 0)
            { //top left corner
                if (temporaryMap[x, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z + 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z + 1] == 1)
                {
                    neighbourCount++;
                }
            }
            else if (x == mapWidth - 1)
            { //top right corner
                if (temporaryMap[x, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z + 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z + 1] == 1)
                {
                    neighbourCount++;
                }
            }
            else
            { //anywhere along the top border
                if (temporaryMap[x, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z + 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z + 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z + 1] == 1)
                {
                    neighbourCount++;
                }
            }
        }
        else if (x == 0)
        {
            if (z == mapLength - 1)
            { //bottom left corner
                if (temporaryMap[x, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z - 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z - 1] == 1)
                {
                    neighbourCount++;
                }
            }
            else
            { //anywhere along the left border.
                if (temporaryMap[x, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z - 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z + 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z + 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z - 1] == 1)
                {
                    neighbourCount++;
                }
            }
        }
        else if (z == mapLength - 1)
        {
            if (x == mapWidth - 1)
            { //bottom right corner
                if (temporaryMap[x, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z - 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z - 1] == 1)
                {
                    neighbourCount++;
                }
            }
            else
            { //anywhere along the bottom border
                if (temporaryMap[x, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x, z - 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x - 1, z - 1] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z] == 1)
                {
                    neighbourCount++;
                }
                if (temporaryMap[x + 1, z - 1] == 1)
                {
                    neighbourCount++;
                }
            }
        }
        else if (x == mapWidth - 1)
        { //anywhere along the right border
            if (temporaryMap[x, z] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x, z + 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x, z - 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x - 1, z] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x - 1, z + 1] == 1)
            {
                neighbourCount++;
            }
            if (temporaryMap[x - 1, z - 1] == 1)
            {
                neighbourCount++;
            }
        }

        return neighbourCount;
    }

    #endregion
}
