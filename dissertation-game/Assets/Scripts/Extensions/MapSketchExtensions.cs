using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Assets.Scripts.Environment.Enums;
using Assets.Scripts.Environment.Helpers;

namespace Assets.Scripts.Extensions
{
    public static class MapSketchExtensions
    {
        public static TileType[,] RemoveUnreachableTiles(this TileType[,] mapSketch, int mapSketchWidth, int mapSketchHeight)
        {
            var newMapSketch = new TileType[mapSketchWidth, mapSketchHeight];
            var spawnPositions = MapSketchHelpers.GetReferenceTilePositionsForSpawns(mapSketchWidth, mapSketchHeight);

            var mapReachableForTeamOne =
                MapSketchHelpers.FloodFillMapSketch(
                    mapSketch,
                    mapSketchWidth,
                    mapSketchHeight,
                    spawnPositions[0]);

            var mapReachableForTeamTwo =
                MapSketchHelpers.FloodFillMapSketch(
                    mapSketch,
                    mapSketchWidth,
                    mapSketchHeight,
                    spawnPositions[1]);

            for (int curX = 0; curX < mapSketchWidth; ++curX)
            {
                for (int curY = 0; curY < mapSketchHeight; ++curY)
                {
                    if (mapReachableForTeamOne[curX, curY] == -1 || mapReachableForTeamTwo[curX, curY] == -1)
                    {
                        newMapSketch[curX, curY] = TileType.Impassable;
                    }
                    else
                    {
                        newMapSketch[curX, curY] = mapSketch[curX, curY];
                    }
                }
            }

            return newMapSketch;
        }
    }
}
