using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GAF;
using GAF.Operators;

namespace Assets.Scripts.Environment.Genetic_Algorithms
{
    public class MapMutate : MutateBase, IGeneticOperator
    {
        public bool Enabled { get; set; }
        public bool RequiresEvaluatedPopulation { get; set; }

        public MapMutate (double mutationProbability) : base (mutationProbability)
        {

        }

        public int GetOperatorInvokedEvaluations()
        {
            throw new NotImplementedException();
        }

        public void Invoke(Population currentPopulation, ref Population newPopulation, FitnessFunction fitnesFunctionDelegate)
        {
            throw new NotImplementedException();
        }

        protected override void Mutate(Chromosome child)
        {
            base.Mutate(child);
        }

        protected override void MutateGene(Gene gene)
        {
            throw new NotImplementedException();
        }
    }
}
