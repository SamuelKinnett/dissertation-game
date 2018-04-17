using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using GAF;

using Assets.Scripts.Environment.Enums;

namespace Assets.Scripts.Environment.Helpers
{
    public static class GeneticAlgorithmHelpers
    {
        public static int mapSketchWidth;
        public static int mapSketchHeight;

        public static float team1TimeRemaining;
        public static float team2TimeRemaining;
        public static float timeToCapture;

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

            var capturePointReachableForTeamOne = targetTiles.Any((tile) => mapReachableForTeamOne[(int)tile.x, (int)tile.y] != -1);
            var capturePointReachableForTeamTwo = targetTiles.Any((tile) => mapReachableForTeamTwo[(int)tile.x, (int)tile.y] != -1);

            if (!capturePointReachableForTeamOne || !capturePointReachableForTeamTwo)
                return 0;

            float totalPassableTiles = 0;

            for (int x = 0; x < mapSketchWidth; ++x)
            {
                for (int y = 0; y < mapSketchHeight; ++y)
                {
                    if (mapSketch[x, y] != TileType.Impassable)
                    {
                        ++totalPassableTiles;
                    }
                }
            }

            var strategicResourceControlForTeam1 = GetStrategicResourceControlValue(0, referenceTiles, targetTiles, mapReachableForTeamOne, mapReachableForTeamTwo);
            var strategicResourceControlForTeam2 = GetStrategicResourceControlValue(1, referenceTiles, targetTiles, mapReachableForTeamTwo, mapReachableForTeamOne);
            var areaControlForTeam1 = GetAreaControlValue(0, referenceTiles, mapSketchWidth, mapSketchHeight, mapReachableForTeamOne, mapReachableForTeamTwo);
            var areaControlForTeam2 = GetAreaControlValue(1, referenceTiles, mapSketchWidth, mapSketchHeight, mapReachableForTeamTwo, mapReachableForTeamOne);
            var explorationForTeam1 = GetMapCoverage(0, referenceTiles, mapSketchWidth, mapSketchHeight, mapSketch, totalPassableTiles) / referenceTiles.Count();
            var explorationForTeam2 = GetMapCoverage(1, referenceTiles, mapSketchWidth, mapSketchHeight, mapSketch, totalPassableTiles) / referenceTiles.Count();

            var team1CapturePercentage = 1 - (team1TimeRemaining / timeToCapture);
            var team2CapturePercentage = 1 - (team2TimeRemaining / timeToCapture);

            var percentageDelta = team1CapturePercentage - team2CapturePercentage;
            var strategicResourceControlDelta = strategicResourceControlForTeam1 - strategicResourceControlForTeam2;

            var fitness = 1.0f - Mathf.Abs(strategicResourceControlDelta + percentageDelta) / 2.0f;

            return fitness;
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
        private static float GetSafetyValue(Vector2 tile, int i, List<Vector2> referenceTiles, int[,] iDistanceMap, int[,] jDistanceMap)
        {
            // We're interested in the lowest value
            float currentLowestSafetyValue = 1;

            for (int j = 0; j < referenceTiles.Count; ++j)
            {
                if (j != i)
                {
                    float distanceToJ = jDistanceMap[(int)tile.x, (int)tile.y];
                    float distanceToI = iDistanceMap[(int)tile.x, (int)tile.y];

                    var safetyValue = Mathf.Max(0, (distanceToJ - distanceToI) / (distanceToJ + distanceToI));
                    if (safetyValue < currentLowestSafetyValue)
                    {
                        currentLowestSafetyValue = safetyValue;
                    }
                }
            }

            return currentLowestSafetyValue;
        }

        /// <summary>
        /// Implementation of Liapsis, Yannakakis and Togelius' map coverage
        /// function.
        /// </summary>
        /// <param name="i">The index of the reference tile to start at.</param>
        /// <param name="referenceTiles">The list of reference tiles.</param>
        /// <param name="mapSketchWidth">The width of the map sketch.</param>
        /// <param name="mapSketchHeight">The height of the map sketch.</param>
        /// <param name="mapSketch">The map sketch.</param>
        /// <param name="totalPassableTiles">The total number of passable tiles for the current map sketch.</param>
        /// <returns></returns>
        private static float GetMapCoverage(int i, List<Vector2> referenceTiles, int mapSketchWidth, int mapSketchHeight, TileType[,] mapSketch, float totalPassableTiles)
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
                            if (mapCoverageToReachTarget[x, y] != -1)
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

        /// <summary>
        /// A slightly modified version of Liapsis, Yannakakis and Togelius'
        /// strategic resource control function, allowing this value to be
        /// calculated on a per-reference tile basis.
        /// </summary>
        /// <param name="i">The index of the reference tile to calculate the strategic resource control value for.</param>
        /// <param name="referenceTiles">The list of reference tiles.</param>
        /// <param name="targetTiles">The list of target tiles.</param>
        /// <param name="graph">The nav graph for this map sketch.</param>
        /// <returns>A value between 0 and 1 indicating the average safety of the target tiles relative to the provided reference tile.</returns>
        private static float GetStrategicResourceControlValue(int i, List<Vector2> referenceTiles, List<Vector2> targetTiles, int[,] iDistanceMap, int[,] jDistanceMap)
        {
            float totalSafetyForReferenceTile = 0;

            for (int k = 0; k < targetTiles.Count; ++k)
            {
                totalSafetyForReferenceTile += GetSafetyValue(targetTiles[k], i, referenceTiles, iDistanceMap, jDistanceMap);
            }

            return totalSafetyForReferenceTile / targetTiles.Count;
        }

        /// <summary>
        /// A slightly modified version of Liapsis, Yannakakis and Togelius'
        /// area control function, allowing this value to be calculated on a
        /// per-reference tile basis.
        /// </summary>
        /// <param name="i">The index of the reference tile to calculate the area control value for.</param>
        /// <param name="referenceTiles">The list of reference tiles.</param>
        /// <param name="mapSketchWidth">The width of the map sketch.</param>
        /// <param name="mapSketchHeight">The height of the mpa sketch.</param>
        /// <param name="mapReachable">The tiles of the map that are reachable for this reference tile.</param>
        /// <param name="graph">The nav graph for this map sketch.</param>
        /// <returns></returns>
        private static float GetAreaControlValue(int i, List<Vector2> referenceTiles, int mapSketchWidth, int mapSketchHeight, int[,] iDistanceMap, int[,] jDistanceMap)
        {
            // The safety value required for a tile to be considered safe relative to i
            const float safetyThreshold = 0.35f;

            float totalPassableTiles = 0;
            float tilesSafeForI = 0;

            for (int x = 0; x < mapSketchWidth; ++x)
            {
                for (int y = 0; y < mapSketchHeight; ++y)
                {
                    if (iDistanceMap[x, y] != -1)
                    {
                        ++totalPassableTiles;
                        var tileSafety = GetSafetyValue(new Vector2(x, y), i, referenceTiles, iDistanceMap, jDistanceMap);

                        if (tileSafety > safetyThreshold)
                        {
                            ++tilesSafeForI;
                        }
                    }
                }
            }

            return tilesSafeForI / totalPassableTiles;
        }
    }
}
