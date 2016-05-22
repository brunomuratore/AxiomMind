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

        /// <summary>
        /// Calculate the best next hint for the previous guesses that were computed by SetResult method.
        /// </summary>
        /// <param name="nPopulation">Number of population of gens</param>
        /// <param name="nGeneration">Number of generations</param>
        /// <returns></returns>
        public int[] CalculateGeneration(int nPopulation, int nGeneration)
        {
            if (CurrentRow < 100)
            {
                MasterMindPopulation TestPopulation = new MasterMindPopulation(nPopulation);
                for (int i = 0; i < nGeneration; i++)
                {
                    TestPopulation.NextGeneration();
                }

                int[] bestGenome = ((MastermindGenome)TestPopulation.GetHighestScoreGenome()).ToArray();
                return bestGenome;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Set the results of a player guess. Must be called on each round that the user plays.
        /// </summary>
        /// <param name="rowIndex">The current round the game is (Zero based index)</param>
        /// <param name="result">The result of the user's guess</param>
        internal void SetResult(int rowIndex, GuessResult result)
        {
            if (rowIndex == 0)
            {
                Grid = new int[8, 100];
                Pegs = new int[8, 100];
            }

            int[] pegs = new int[8];
            int idx = 0;
            for (int i = 0; i < result.Exactly; i++)
            {
                pegs[idx] = 1;
                idx++;
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
                int gridValue = Convert.ToInt32(result.Guess[i].ToString());
                Grid[i, rowIndex] = gridValue;
                Pegs[i, rowIndex] = pegs[i];
            }
            CurrentRow = rowIndex;
        }
    }
}
