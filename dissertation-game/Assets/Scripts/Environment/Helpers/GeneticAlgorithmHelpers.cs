using System;
using System.Collections.Generic;
using System.Linq;

using Assets.Scripts.Environment.Enums;

using GAF;
using UnityEngine;

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

            // Filter out impossible maps
            // TODO: find a more efficient way of finding stuff than this mess
            Vector2? teamOneSpawn = null;
            Vector2? teamTwoSpawn = null;
            var captureZone = new List<Vector2>();

            for (int curX = 0; curX < mapSketchWidth; ++curX)
            {
                for (int curY = 0; curY < mapSketchHeight; ++curY)
                {
                    if (mapSketch[curX, curY] == TileType.Team1Spawn)
                    {
                        if (!teamOneSpawn.HasValue)
                        {
                            teamOneSpawn = new Vector2(curX, curY);
                        }
                    }
                    else if (mapSketch[curX, curY] == TileType.Team2Spawn)
                    {
                        if (!teamTwoSpawn.HasValue)
                        {
                            teamTwoSpawn = new Vector2(curX, curY);
                        }
                    }
                    else if (mapSketch[curX, curY] == TileType.CapturePoint)
                    {
                        captureZone.Add(new Vector2(curX, curY));
                    }
                }
            }

            var mapReachableForTeamOne =
                MapSketchHelpers.FloodFillMapSketch(
                    mapSketch,
                    mapSketchWidth,
                    mapSketchHeight,
                    (int)teamOneSpawn.Value.x,
                    (int)teamOneSpawn.Value.y);

            var mapReachableForTeamTwo =
                MapSketchHelpers.FloodFillMapSketch(
                    mapSketch,
                    mapSketchWidth,
                    mapSketchHeight,
                    (int)teamTwoSpawn.Value.x,
                    (int)teamTwoSpawn.Value.y);

            var capturePointReachableForTeamOne = captureZone.Any((tile) => mapReachableForTeamOne[(int)tile.x, (int)tile.y]);
            var capturePointReachableForTeamTwo = captureZone.Any((tile) => mapReachableForTeamTwo[(int)tile.x, (int)tile.y]);

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
    }
}
