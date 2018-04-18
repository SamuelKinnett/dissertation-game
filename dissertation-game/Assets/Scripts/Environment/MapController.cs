using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Networking;

using Assets.Scripts.Environment.Enums;
using Assets.Scripts.Environment.Genetic_Algorithms;
using Assets.Scripts.Environment.Helpers;
using Assets.Scripts.Environment.Structs;
using Assets.Scripts.Player.Enums;

using GAF;
using GAF.Operators;

using Newtonsoft.Json;

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

    // How long to preview the map changes before they are made concrete
    [SyncVar]
    public float previewTime;

    // The X, Y and Z dimensions of the chunks
    public int chunkWidth;
    public int chunkHeight;
    public int chunkDepth;

    // The map chunk prefab
    public GameObject mapChunkPrefab;

    // The capture point prefab
    public GameObject capturePointPrefab;

    // Reference to the main camera so it can be positioned in the centre of 
    // the map.
    public Camera MainCamera;

    // This controller generates some nice surrounding terrain around the map
    public BorderTerrainController borderTerrainController;

    // This SyncList stores the tuples that form the genotype
    private SyncListGeneTuple currentGenes = new SyncListGeneTuple();

    // These arrays store the data of the map geometry, with each element 
    // representing one 'block'.A 0 represents empty space, numbers above this
    // represent different block types.
    private static byte[,,] mapData;

    private TileType[,] currentMapSketch;

    private static List<MapChunkController> mapChunks;

    private static int wallHeight = 4;

    private bool chunksInitialised;

    // Reference to the capture point once spawned in
    private CapturePointController capturePoint;

    // Spawning related variables
    private List<Vector3> redTeamSpawnPositions;
    private List<Vector3> blueTeamSpawnPositions;
    private int currentRedTeamSpawnIndex;
    private int currentBlueTeamSpawnIndex;

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

    public Vector3 GetSpawnPositionForTeam(Team team)
    {
        switch (team)
        {
            case Team.Red:
                if (currentRedTeamSpawnIndex < redTeamSpawnPositions.Count)
                {
                    ++currentRedTeamSpawnIndex;
                }
                else
                {
                    currentRedTeamSpawnIndex = 0;
                }
                return redTeamSpawnPositions[currentRedTeamSpawnIndex];

            case Team.Blue:
                if (currentBlueTeamSpawnIndex < blueTeamSpawnPositions.Count)
                {
                    ++currentBlueTeamSpawnIndex;
                }
                else
                {
                    currentBlueTeamSpawnIndex = 0;
                }
                return blueTeamSpawnPositions[currentBlueTeamSpawnIndex];

            case Team.Random:
            default:
                // We should never reach this, but just in case return a
                // default vector. A future improvement could be to allow for
                // deathmatch gamemodes with random spawns.
                return Vector3.zero;

        }
    }

    public bool GetHasSpawnPositions(Team team)
    {
        switch (team)
        {
            case Team.Red:
                return redTeamSpawnPositions.Count > 0;

            case Team.Blue:
                return blueTeamSpawnPositions.Count > 0;

            case Team.Random:
            default:
                return false;
        }
    }

    private void UpdateMapWithCurrentGenes()
    {
        // if the map hasn't yet been instantiated due to a race ocndition.
        // delay this method.
        if (!chunksInitialised)
        {
            Debug.Log("Delaying map update...");
            Invoke("UpdateMapWithCurrentGenes", 1);
        }
        else
        {
            currentMapChromosome = new Chromosome(currentGenes.Select((geneTuple) => new Gene(geneTuple)));
            Debug.Log("Updated current chromosome");

            currentMapSketch = MapSketchHelpers.ConvertChromosomeToMapSketch(currentMapChromosome, (int)mapDimensions.x / 2, (int)mapDimensions.z / 2);
            UpdateMapWithMapSketch(currentMapSketch);
            Debug.Log("Updated map");
        }
    }

    public void UpdateMapWithMapSketch(TileType[,] mapSketch)
    {
        var mapSketchWidth = (int)mapDimensions.x / 2;
        var mapSketchHeight = (int)mapDimensions.z / 2;

        var newRedTeamSpawnPositions = new List<Vector3>();
        var newBlueTeamSpawnPositions = new List<Vector3>();

        for (int curX = 0; curX < mapSketchWidth; ++curX)
        {
            for (int curY = 0; curY < mapSketchHeight; ++curY)
            {
                switch (mapSketch[curX, curY])
                {
                    case TileType.Impassable:
                        for (int i = 0; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, BlockType.BrickWall2);
                        }
                        break;

                    case TileType.Passable:
                        SetLargeBlock(curX, 0, curY, BlockType.TileFloor);
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
                        newRedTeamSpawnPositions.Add(new Vector3(curX * 2 + 1, 3, curY * 2 + 1));
                        break;

                    case TileType.Team2Spawn:
                        SetLargeBlock(curX, 0, curY, BlockType.Team2Spawn);
                        for (int i = 1; i < wallHeight; ++i)
                        {
                            SetLargeBlock(curX, i, curY, 0);
                        }
                        newBlueTeamSpawnPositions.Add(new Vector3(curX * 2 + 1, 3, curY * 2 + 1));
                        break;

                    case TileType.Barrier:
                        SetLargeBlock(curX, 0, curY, BlockType.BrickWall2);
                        SetLargeBlock(curX, 1, curY, BlockType.BrickWall2);
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
                            SetLargeBlock(curX, i, curY, BlockType.BrickWall2);
                        }
                        break;
                }
            }
        }

        // Update the capture point
        Invoke("UpdateCapturePoint", previewTime);

        redTeamSpawnPositions = newRedTeamSpawnPositions;
        blueTeamSpawnPositions = newBlueTeamSpawnPositions;

        GenerateMesh();
    }

    [ServerCallback]
    public void MovePlayerToNearestSafeTile(Player player)
    {
        // Debug.Log($"Warping player {player.PlayerName}");

        var safeTiles = new List<Vector2>();

        var playerPosition = new Vector2(
            (int)Mathf.Round(player.transform.position.x / 2),
            (int)Mathf.Round(player.transform.position.z / 2));

        // Find all empty tiles
        for (int x = 0; x < (int)mapDimensions.x / 2; ++x)
        {
            for (int y = 0; y < (int)mapDimensions.z / 2; ++y)
            {
                if (currentMapSketch[x, y] != TileType.Impassable)
                {
                    safeTiles.Add(new Vector2(x, y));
                }
            }
        }

        // Find the closest safe tile
        safeTiles.Sort((tile1, tile2) =>
        {
            var distanceToTile1 = (tile1 - playerPosition).magnitude;
            var distanceToTile2 = (tile2 - playerPosition).magnitude;

            return distanceToTile1.CompareTo(distanceToTile2);
        });

        var closestTile = safeTiles.First();

        player.RpcWarpToPosition(new Vector3(closestTile.x * 2 + 1, wallHeight, closestTile.y * 2 + 1));
    }

    public bool CheckPlayerInSafeTile(Player player)
    {
        if (isServer)
        {
            var playerTileX = (int)Mathf.Round(player.transform.position.x / 2);
            var playerTileY = (int)Mathf.Round(player.transform.position.z / 2);

            // Debug.Log($"Checking player {player.PlayerName} at [{playerTileX}, {playerTileY}] ({currentMapSketch[playerTileX, playerTileY].ToString()})");

            return !(currentMapSketch[playerTileX, playerTileY] == TileType.Impassable);
        }

        return true;
    }

    // Use this for initialization
    private void Start()
    {
        updatingGenes = true;
        chunksInitialised = false;

        mapData = new byte[(int)mapDimensions.x, (int)mapDimensions.y, (int)mapDimensions.z];
        mapChunks = new List<MapChunkController>();
        InstantiateChunks();

        borderTerrainController.GenerateTerrain((int)mapDimensions.x / 2, (int)mapDimensions.z / 2, wallHeight * 2 - 1);

        capturePoint = capturePointPrefab.GetComponent<CapturePointController>();

        redTeamSpawnPositions = new List<Vector3>();
        blueTeamSpawnPositions = new List<Vector3>();

        mapUpdateNeeded = false;

        MainCamera.transform.position = new Vector3(mapDimensions.x / 2, MainCamera.transform.position.y, mapDimensions.z / 2);

        if (isServer)
        {
            GeneticAlgorithmHelpers.mapSketchWidth = (int)mapDimensions.x / 2;
            GeneticAlgorithmHelpers.mapSketchHeight = (int)mapDimensions.z / 2;
            GeneticAlgorithmHelpers.timeToCapture = GameTimeManager.Instance.RequiredHoldTime;

            // Create the default genes
            for (int i = 0; i < 31; ++i)
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
            currentGenes[30] = new GeneTuple((int)mapDimensions.x / 4 - 1, (int)mapDimensions.z / 4 - 1, 2);
            updatingGenes = false;

            // TODO: Move this into a game manager class
            GameTimeManager.Instance.SetGameTimerPaused(false);
            PlayerCanvasController.Instance.SetHidePlayerSpecificElements(true);

            currentMapChromosome = new Chromosome(currentGenes.Select((geneTuple) => new Gene(geneTuple)));
            mapUpdateNeeded = true;
        }
    }

    private void Update()
    {
        if (isServer && mapUpdateNeeded)
        {
            // Update the current gene list for network synchronisation
            updatingGenes = true;

            for (int i = 0; i < currentMapChromosome.Genes.Count; ++i)
            {
                var geneValue = (GeneTuple)currentMapChromosome.Genes[i].ObjectValue;

                if (currentGenes[i] != geneValue)
                {
                    currentGenes[i] = geneValue;
                }
            }

            updatingGenes = false;
            mapUpdateNeeded = false;

            // Store the map in the database
            DatabaseManager.Instance.InsertNewMap(JsonConvert.SerializeObject(currentGenes));

            if (!isClient)
            {
                // If this is a dedicated server, make sure to update the map
                UpdateMapWithCurrentGenes();
            }

            if (!GameTimeManager.Instance.GameTimerPaused &&
                GameTimeManager.Instance.RedTeamCaptureTimeRemaining > 0 &&
                GameTimeManager.Instance.BlueTeamCaptureTimeRemaining > 0 &&
                GameInstanceData.Instance.GameType == GameType.Procedural)
            {
                Invoke("GenerateWorld", 15);
            }
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

        chunksInitialised = true;
        Debug.Log("Chunks Instantiated");
    }

    private void GenerateMesh()
    {
        foreach (var currentChunk in mapChunks)
        {
            currentChunk.GenerateMesh(previewTime);
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

        // Update the game time variables in the genetic algorithm helpers
        GeneticAlgorithmHelpers.team1TimeRemaining = GameTimeManager.Instance.RedTeamCaptureTimeRemaining;
        GeneticAlgorithmHelpers.team2TimeRemaining = GameTimeManager.Instance.BlueTeamCaptureTimeRemaining;

        // Run the genetic algorithm
        geneticAlgorithm.RunAsync(GeneticAlgorithmHelpers.Terminate);
    }

    private void UpdateCapturePoint()
    {
        var capturePointGene = currentGenes.Last();
        capturePoint.UpdateCapturePoint(
            new Vector3(
            (capturePointGene.X + capturePointGene.Z / 2.0f) * 2,
            10,
            (capturePointGene.Y + capturePointGene.Z / 2.0f) * 2),
            new Vector3(capturePointGene.Z * 2, 20, capturePointGene.Z * 2));
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
