using System;
using System.Collections.Generic;

using Assets.Scripts.Environment.Enums;

using GAF;
using UnityEngine;

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
                var geneTuple = (Tuple<int, int, int>)newChromosome.Genes[currentGeneIndex].ObjectValue;
                if (currentGeneIndex < 15)
                {
                    // This gene is an arena
                    int arenaX = geneTuple.Item1;
                    int arenaY = geneTuple.Item2;
                    int arenaSize = Mathf.Abs(geneTuple.Item3);
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
                                        curX < mapSketchWidth &&
                                        corridorY + i < mapSketchHeight)
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
                                    curX < mapSketchWidth &&
                                    corridorY < mapSketchHeight)
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
                                        curY < mapSketchHeight &&
                                        corridorX + i < mapSketchWidth)
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
                                    curY < mapSketchHeight &&
                                    corridorX < mapSketchWidth)
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

                    mapSketch[pickupX, pickupY] = TileType.HealthPickup;
                }
                else
                {
                    // This gene is the capture point
                    var capturePointX = geneTuple.Item1;
                    var capturePointY = geneTuple.Item2;
                    var capturePointSize = geneTuple.Item3;

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
            for (int curY = 1; curY < mapSketchHeight - 2; ++curY)
            {
                mapSketch[0, curY] = TileType.Impassable;
                mapSketch[mapSketchWidth - 1, curY] = TileType.Impassable;
            }

            return mapSketch;
        }

        public static bool[,] FloodFillMapSketch(TileType[,] mapSketch, int mapSketchWidth, int mapSketchHeight, int startX, int startY)
        {
            // This queue stores all tiles currently being considered
            Queue<Tuple<int, int>> tiles = new Queue<Tuple<int, int>>();

            // This array stores true if a tile can be reached from the starting tile
            bool[,] reachableTiles = new bool[mapSketchWidth, mapSketchHeight];

            // This array stores true if a tile has been visited already by the algorithm
            bool[,] visitedTiles = new bool[mapSketchWidth, mapSketchHeight];

            // Initialise the queue
            tiles.Enqueue(new Tuple<int, int>(startX, startY));
            visitedTiles[startX, startY] = true;

            while (tiles.Count > 0)
            {
                var currentTile = tiles.Dequeue();

                var x = (int)currentTile.Item1;
                var y = (int)currentTile.Item2;

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
    }
}
