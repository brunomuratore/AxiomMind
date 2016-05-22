using System;
using System.Collections;

namespace AxiomMind.Bot
{
	public class MastermindGenome : ListGenome
    {

        public MastermindGenome()
		{
		}

		public MastermindGenome(long length, object min, object max) : base(length, min, max)
		{
		}

		private int[] GetIntArray(ArrayList a)
		{
			int[] result = new int[8]{0,0,0,0,0,0,0,0};
			for (int i = 0; i < a.Count; i++)
			{
				result[i] = (int)a[i];
			}

			return result;
		}
        
		private int CompareToScore(int nCompareRow, int[] testpegs)
		{
			int nScoreMatch = 0;
			for (int i = 0; i < 8; i++)
			{
				int nPeg = GetPeg(i, nCompareRow);
				if (testpegs[i] == nPeg)
				{
					nScoreMatch++;
				}
			}

			return nScoreMatch;
		}

        private int GetPeg(int i, int nCompareRow)
        {
            return AxiomBot.Pegs[i, nCompareRow];
        }

        private float CalculateFromMastermindBoard()
		{

			float fFitnessScore = 0.0f;
			for (int i = 0; i < AxiomBot.CurrentRow; i++)
			{
				int[] result = CalcScore(GetIntArray(TheArray), i); 	
				int numCorrectInRow = CompareToScore(i, result);
				fFitnessScore += ((float)numCorrectInRow)/8.0f;
			}

			fFitnessScore += .02f;

			return fFitnessScore;
		}

		public override float CalculateFitness()
		{
		    CurrentFitness = CalculateFromMastermindBoard();
			return CurrentFitness;
		}
        
		public override void CopyGeneInfo(Genome dest)
		{
			MastermindGenome theGene = (MastermindGenome)dest;
			theGene.Length = Length;
			theGene.TheMin = TheMin;
			theGene.TheMax = TheMax;
		}


		public override Genome Crossover(Genome g)
		{
			MastermindGenome aGene1 = new MastermindGenome();
			MastermindGenome aGene2 = new MastermindGenome();
			g.CopyGeneInfo(aGene1);
			g.CopyGeneInfo(aGene2);


			MastermindGenome CrossingGene = (MastermindGenome)g;
			for (int i = 0; i < CrossoverPoint; i++)
			{
				aGene1.TheArray.Add(CrossingGene.TheArray[i]);
				aGene2.TheArray.Add(TheArray[i]);
			}

			for (int j = CrossoverPoint; j < Length; j++)
			{
				aGene1.TheArray.Add(TheArray[j]);
				aGene2.TheArray.Add(CrossingGene.TheArray[j]);
			}

			// 50/50 chance of returning gene1 or gene2
			MastermindGenome aGene = null;
			if (TheSeed.Next(2) == 1)			
			{
				aGene = aGene1;
			}
			else
			{
				aGene = aGene2;
			}

			return aGene;
		}

        public int[] CalcScore(int[] autoGuess, int row)
        {
            int nExact = 0;
            int nThere = 0;
            int nCount = 0;
            int[] places = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
            int[] places2 = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
            int[] result = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

            // do exact first
            for (int i = 0; i < 8; i++)
            {
                if (autoGuess[i] == AxiomBot.Grid[i, row])
                {
                    nExact++;
                    result[nCount] = 1;
                    nCount++;
                    places[i] = 1;
                    places2[i] = 1;
                }
            }

            if (nExact == 8)
            {
                return result;
            }

            // now do there
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if ((i != j) && (places[i] != 1) && (places2[j] != 1))
                    {
                        if (autoGuess[i] == AxiomBot.Grid[j, row])
                        {
                            nThere++;
                            result[nCount] = 2;
                            nCount++;
                            places[i] = 1;
                            places2[j] = 1;
                            j = 9;  
                        }
                    }
                }
            }

            return result;

        }

    }
}
