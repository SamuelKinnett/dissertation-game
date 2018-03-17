using GAF;
using GAF.Operators;

using Assets.Scripts.Environment.Structs;

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
            // Randomly choose a new x and y value
            var rand = new System.Random();

            var newX = rand.Next(0, mapWidth);
            var newY = rand.Next(0, mapHeight);

            // Randomly choose a new z value, first deciding whether to use a
            // 'vertical' or 'horizontal' value
            var newZ = rand.Next(0, 2) == 0 
                ? rand.Next(0, mapWidth - newX)     // Horizontal
                : rand.Next(-mapHeight + newY, 1);  // Vertical

            gene.ObjectValue = new GeneTuple(newX, newY, newZ);
        }
    }
}
