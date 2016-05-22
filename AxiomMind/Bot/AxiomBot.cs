using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AxiomMind.Models;

namespace AxiomMind.Bot
{
    public class AxiomBot
    {
        Population TheGenePopulation = new MasterMindPopulation();
        public static int[,] Grid = new int[8, 100];
        public static int CurrentRow = 0;
        public static int[,] Pegs = new int[8, 100];

        public int[] CalculateGeneration(int nPopulation, int nGeneration)
        {
            MasterMindPopulation TestPopulation = new MasterMindPopulation(nPopulation);
            for (int i = 0; i < nGeneration; i++)
            {
                TestPopulation.NextGeneration();
            }

            int[] bestGenome = ((MastermindGenome)TestPopulation.GetHighestScoreGenome()).ToArray();
            return bestGenome;
        }

        internal void SetResult(int rowIndex, GuessResult result)
        {
            int[] pegs = new int[8];
            int idx = 0;
            for(int i = 0; i < result.Exactly; i++)
            {
                pegs[idx] = 1;
                idx ++;
            }
            for (int i = 0; i < result.Near; i++)
            {
                pegs[idx] = 2;
                idx++;
            }
            for (int i = 0; i < 8 - result.Exactly - result.Near; i++)
            {
                pegs[idx] = 0;
                idx++;
            }

            for (int i = 0; i < 8; i++)
            {
                int gridValue = Convert.ToInt32(result.Guess[i]);
                Grid[i, rowIndex] = gridValue;
                Pegs[i, rowIndex] = pegs[i];                
            }
            CurrentRow = rowIndex;
        }
    }
}
