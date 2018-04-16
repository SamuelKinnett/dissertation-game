using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using GAF;

using Assets.Environment.Helpers.Pathfinding;
using Assets.Scripts.Environment.Enums;
using Assets.Scripts.Environment.Structs;

namespace Assets.Scripts.Environment.Helpers
{
    /// <summary>
    /// Static helper functions related to map sketches
    /// </summary>
    public static class MapSketchHelpers
    {
        /// <summary>
        /// Converts a chromosome into a map sketch
        /// </summary>
        public static TileType[,] ConvertChromosomeToMapSketch(Chromosome newChromosome, int mapSketchWidth, int mapSketchHeight)
        {
            // Create a new map sketch filled with impassable tiles
            var mapSketch = new TileType[mapSketchWidth, mapSketchHeight];

            // Build the map
            for (int currentGeneIndex = 0; currentGeneIndex < newChromosome.Genes.Count; ++currentGeneIndex)
            {
                var geneTuple = (GeneTuple)newChromosome.Genes[currentGeneIndex].ObjectValue;
                if (currentGeneIndex < 15)
                {
                    // This gene is an arena
                    int arenaX = geneTuple.X;
                    int arenaY = geneTuple.Y;
                    int arenaSize = Mathf.Abs(geneTuple.Z);
                    for (int curX = arenaX; curX < arenaX + arenaSize; ++curX)
                    {
                        for (int curY = arenaY; curY < arenaY + arenaSize; ++curY)
                        {
                            if (curX >= 0 &&
                                curY >= 0 &&
                                curX < mapSketchWidth &&
                                curY < mapSketchHeight)
                            {
                                mapSketch[curX, curY] = TileType.Passable;
                            }
                        }
                    }
                }
                else if (currentGeneIndex < 30)
                {
                    // This gene is a corridor
                    int corridorX = geneTuple.X;
                    int corridorY = geneTuple.Y;
                    int corridorLength = geneTuple.Z;

                    if (corridorLength > 1)
                    {
                        // Horizontal corridor
                        for (int curX = corridorX; curX < corridorX + corridorLength; ++curX)
                        {
                            // This is a corridor
                            // All corridors have a fixed width of three tiles
                            for (int i = -1; i < 2; ++i)
                            {
                                if (curX >= 0 &&
                                    corridorY + i >= 0 &&
                                    curX < mapSketchWidth &&
                                    corridorY + i < mapSketchHeight)
                                {
                                    mapSketch[curX, corridorY + i] = TileType.Passable;
                                }
                            }
                        }
                    }
                    else
                    {
                        // Vertical corridor
                        for (int curY = corridorY; curY < corridorY - corridorLength; ++curY)
                        {
                            // This is a corridor
                            // All corridors have a fixed width of three tiles
                            for (int i = -1; i < 2; ++i)
                            {
                                if (curY >= 0 &&
                                    corridorX + i >= 0 &&
                                    curY < mapSketchHeight &&
                                    corridorX + i < mapSketchWidth)
                                {
                                    mapSketch[corridorX + i, curY] = TileType.Passable;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // This gene is the capture point
                    var capturePointX = geneTuple.X;
                    var capturePointY = geneTuple.Y;
                    var capturePointSize = geneTuple.Z;

                    for (int curX = capturePointX; curX < capturePointX + capturePointSize; ++curX)
                    {
                        for (int curY = capturePointY; curY < capturePointY + capturePointSize; ++curY)
                        {
                            if (curX >= 0 &&
                                curX < mapSketchWidth &&
                                curY >= 0 &&
                                curY < mapSketchHeight)
                            {
                                mapSketch[curX, curY] = TileType.CapturePoint;
                            }
                        }
                    }
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
            for (int spawnX = mapSketchWidth - 7; spawnX < mapSketchWidth - 2; ++spawnX)
            {
                for (int spawnY = mapSketchHeight - 7; spawnY < mapSketchHeight - 2; ++spawnY)
                {
                    mapSketch[spawnX, spawnY] = TileType.Team2Spawn;
                }
            }

            // Add in the default border wall
            for (int curX = 0; curX < mapSketchWidth; ++curX)
            {
                mapSketch[curX, 0] = TileType.Impassable;
                mapSketch[curX, mapSketchHeight - 1] = TileType.Impassable;
            }
            for (int curY = 1; curY < mapSketchHeight - 1; ++curY)
            {
                mapSketch[0, curY] = TileType.Impassable;
                mapSketch[mapSketchWidth - 1, curY] = TileType.Impassable;
            }

            return mapSketch;
        }

        public static Vector2[] GetReferenceTilePositionsForSpawns(int mapSketchWidth, int mapSketchHeight)
        {
            // For simplicity, we assume there is a single reference tile for
            // each team, wich we take to be the central tile of the spawn
            // point.
            var returnList = new Vector2[2];

            returnList[0].x = 5;
            returnList[0].y = 5;
            returnList[1].x = mapSketchWidth - 5;
            returnList[1].y = mapSketchHeight - 5;

            return returnList;
        }

        public static List<Vector2> GetTargetTiles(Chromosome chromosome, int mapSketchWidth, int mapSketchHeight)
        {
            var geneTuple = (GeneTuple)chromosome.Genes.Last().ObjectValue;
            var returnList = new List<Vector2>();

            var capturePointX = geneTuple.X;
            var capturePointY = geneTuple.Y;
            var capturePointSize = geneTuple.Z;

            for (int curX = capturePointX; curX < capturePointX + capturePointSize; ++curX)
            {
                for (int curY = capturePointY; curY < capturePointY + capturePointSize; ++curY)
                {
                    if (curX >= 0 &&
                        curX < mapSketchWidth &&
                        curY >= 0 &&
                        curY < mapSketchHeight)
                    {
                        returnList.Add(new Vector2(curX, curY));
                    }
                }
            }

            return returnList;
        }

        public static bool[,] FloodFillMapSketch(TileType[,] mapSketch, int mapSketchWidth, int mapSketchHeight, Vector2 startingTile, Vector2? targetTile = null)
        {
            // This queue stores all tiles currently being considered
            Queue<Tuple<int, int>> tiles = new Queue<Tuple<int, int>>();

            // This array stores true if a tile can be reached from the starting tile
            bool[,] reachableTiles = new bool[mapSketchWidth, mapSketchHeight];

            // This array stores true if a tile has been visited already by the algorithm
            bool[,] visitedTiles = new bool[mapSketchWidth, mapSketchHeight];

            // Initialise the queue
            tiles.Enqueue(new Tuple<int, int>((int)startingTile.x, (int)startingTile.y));
            visitedTiles[(int)startingTile.x, (int)startingTile.y] = true;

            bool exitEarly = false;

            while (tiles.Count > 0 && !exitEarly)
            {
                var currentTile = tiles.Dequeue();

                var x = (int)currentTile.Item1;
                var y = (int)currentTile.Item2;

                if (targetTile.HasValue)
                {
                    // If a target has been specified and we've reached it then
                    // exit early.
                    if (x == (int)targetTile.Value.x && y == (int)targetTile.Value.y)
                    {
                        exitEarly = true;
                    }
                }

                // If this tile isn't impassable then add its neighbours to
                // the queue
                if (mapSketch[x, y] != TileType.Impassable)
                {
                    reachableTiles[x, y] = true;

                    if (x + 1 < mapSketchWidth - 1 &&
                        !visitedTiles[x + 1, y])
                    {
                        tiles.Enqueue(new Tuple<int, int>(x + 1, y));
                        visitedTiles[x + 1, y] = true;
                    }
                    if (x - 1 > 0 &&
                        !visitedTiles[x - 1, y])
                    {
                        tiles.Enqueue(new Tuple<int, int>(x - 1, y));
                        visitedTiles[x - 1, y] = true;
                    }
                    if (y + 1 < mapSketchHeight - 1 &&
                        !visitedTiles[x, y + 1])
                    {
                        tiles.Enqueue(new Tuple<int, int>(x, y + 1));
                        visitedTiles[x, y + 1] = true;
                    }
                    if (y - 1 > 0 &&
                        !visitedTiles[x, y - 1])
                    {
                        tiles.Enqueue(new Tuple<int, int>(x, y - 1));
                        visitedTiles[x, y - 1] = true;
                    }
                }
            }

            return reachableTiles;
        }

        public static Graph CreateGraphForMapSketch(TileType[,] mapSketch, int mapSketchWidth, int mapSketchHeight)
        {
            var newGraph = new Graph();

            // Add all passable tiles to the graph
            for (int x = 0; x < mapSketchWidth; ++x)
            {
                for (int y = 0; y < mapSketchHeight; ++y)
                {
                    if (mapSketch[x, y] != TileType.Impassable)
                    {
                        newGraph.AddNode(x, y);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x - 1, y), true);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x - 1, y - 1), true);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x, y - 1), true);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x + 1, y - 1), true);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x + 1, y), true);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x + 1, y + 1), true);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x, y + 1), true);
                        newGraph.AddEdge(new Vector2(x, y), new Vector2(x - 1, y + 1), true);
                    }
                }
            }

            return newGraph;
        }
    }
}
