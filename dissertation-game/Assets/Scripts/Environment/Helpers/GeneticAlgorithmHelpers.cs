using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using GAF;

using Assets.Environment.Helpers.Pathfinding;
using Assets.Scripts.Environment.Enums;

namespace Assets.Scripts.Environment.Helpers
{
    public static class GeneticAlgorithmHelpers
    {
        public static int mapSketchWidth;
        public static int mapSketchHeight;

        public static double FitnessFunction(Chromosome chromosome)
        {
            var mapSketch = MapSketchHelpers.ConvertChromosomeToMapSketch(chromosome, mapSketchWidth, mapSketchHeight);

            // If the map contains no capture zone, reject it
            if (!mapSketch.Cast<TileType>().Any((tile) => tile == TileType.CapturePoint))
                return 0;

            // Player spawn tiles
            var referenceTiles = MapSketchHelpers.GetReferenceTilePositionsForSpawns(mapSketchWidth, mapSketchHeight).ToList();
            // The capture point tiles
            var targetTiles = MapSketchHelpers.GetTargetTiles(chromosome, mapSketchWidth, mapSketchHeight);

            // Filter out impossible maps
            var mapReachableForTeamOne =
                MapSketchHelpers.FloodFillMapSketch(
                    mapSketch,
                    mapSketchWidth,
                    mapSketchHeight,
                    referenceTiles[0]);

            var mapReachableForTeamTwo =
                MapSketchHelpers.FloodFillMapSketch(
                    mapSketch,
                    mapSketchWidth,
                    mapSketchHeight,
                    referenceTiles[1]);

            var capturePointReachableForTeamOne = targetTiles.Any((tile) => mapReachableForTeamOne[(int)tile.x, (int)tile.y]);
            var capturePointReachableForTeamTwo = targetTiles.Any((tile) => mapReachableForTeamTwo[(int)tile.x, (int)tile.y]);

            if (!capturePointReachableForTeamOne || !capturePointReachableForTeamTwo)
                return 0;

            //var testFitness = (mapReachableForTeamOne.Cast<bool>().Count((tile) => tile) + mapReachableForTeamTwo.Cast<bool>().Count((tile) => tile)) / (float)(mapSketchWidth * mapSketchHeight * 2);
            var testRand = new System.Random();
            var testFitness = testRand.NextDouble();

            return testFitness;
        }

        public static bool Terminate(Population population, int currentGeneration, long currentEvaluation)
        {
            return currentGeneration > 10;
        }

        /// <summary>
        /// Implementation of Liapsis, Yannakakis and Togelius' safety
        /// function.
        /// </summary>
        /// <param name="tile">The tile to calculate the safety value for.</param>
        /// <param name="i">The index of the reference tile to calculate the safety value relative to.</param>
        /// <param name="referenceTiles">The list of reference tiles.</param>
        /// <param name="graph">The nav graph for this map sketch.</param>
        /// <returns></returns>
        private static float GetSafetyValue(Vector2 tile, int i, List<Vector2> referenceTiles, Graph graph)
        {
            // We're interested in the lowest value
            float currentLowestSafetyValue = 1;

            for (int j = 0; j < referenceTiles.Count; ++j)
            {
                if (j != i)
                {
                    float distanceToJ;
                    float distanceToI;

                    if (graph.GetDistance(tile, referenceTiles[j], out distanceToJ) && graph.GetDistance(tile, referenceTiles[i], out distanceToI))
                    {
                        var safetyValue = Mathf.Max(0, (distanceToJ - distanceToI) / (distanceToJ + distanceToI));
                        if (safetyValue < currentLowestSafetyValue)
                        {
                            currentLowestSafetyValue = safetyValue;
                        }
                    }
                }
            }

            return currentLowestSafetyValue;
        }

        /// <summary>
        /// Implementation of Liapsis, Yannakakis and Togelius' exploration
        /// function.
        /// </summary>
        /// <param name="i">The index of the reference tile to start at.</param>
        /// <param name="referenceTiles">The list of reference tiles.</param>
        /// <param name="mapSketchWidth">The width of the map sketch.</param>
        /// <param name="mapSketchHeight">The height of the map sketch.</param>
        /// <param name="mapSketch">The map sketch.</param>
        /// <param name="totalPassableTiles">The total number of passable tiles for the current map sketch.</param>
        /// <returns></returns>
        private static float GetExplorationValue(int i, List<Vector2> referenceTiles, int mapSketchWidth, int mapSketchHeight, TileType[,] mapSketch, float totalPassableTiles)
        {
            float totalMapCoverageToReachEachReferenceTile = 0f;

            for (int j = 0; j < referenceTiles.Count; ++j)
            {
                if (j != i)
                {
                    var mapCoverageToReachTarget = MapSketchHelpers.FloodFillMapSketch(mapSketch, mapSketchWidth, mapSketchHeight, referenceTiles[i], referenceTiles[j]);
                    var totalTilesToReachTarget = 0;
                    for (int x = 0; x < mapSketchWidth; ++x)
                    {
                        for (int y = 0; y < mapSketchHeight; ++y)
                        {
                            if (mapCoverageToReachTarget[x, y])
                            {
                                ++totalTilesToReachTarget;
                            }
                        }
                    }
                    totalMapCoverageToReachEachReferenceTile += totalTilesToReachTarget / totalPassableTiles;
                }
            }

            return (1 / (referenceTiles.Count - 1)) * totalMapCoverageToReachEachReferenceTile;
        }
    }
}
