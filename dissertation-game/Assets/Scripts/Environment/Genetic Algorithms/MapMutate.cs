using System;

using GAF;
using GAF.Operators;
using UnityEngine;

namespace Assets.Scripts.Environment.Genetic_Algorithms
{
    public class MapMutate : MutateBase, IGeneticOperator
    {
        private int mapWidth;
        private int mapHeight;

        public MapMutate (double mutationProbability, int mapWidth, int mapHeight) : base (mutationProbability)
        {
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
        }

        protected override void Mutate(Chromosome child)
        {
            base.Mutate(child);
        }

        protected override void MutateGene(Gene gene)
        {
            var rand = new System.Random();

            gene.ObjectValue = new Tuple<int, int, int>(rand.Next(0, mapWidth), rand.Next(0, mapHeight), rand.Next(0, Mathf.Min(mapWidth, mapHeight)));
        }
    }
}
