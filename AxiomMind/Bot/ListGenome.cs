using System;
using System.Collections;

namespace AxiomMind.Bot
{
	/// <summary>
	/// Summary description for ListGenome.
	/// </summary>
	public class ListGenome : Genome
	{
		protected ArrayList TheArray = new ArrayList();
		public static Random TheSeed = new Random((int)DateTime.Now.Ticks);
		protected int TheMin = 0;
		protected int TheMax = 100;

		public override int CompareTo(object a)
		{
			ListGenome Gene1 = this;
			ListGenome Gene2 = (ListGenome)a;
			return Math.Sign(Gene2.CurrentFitness  -  Gene1.CurrentFitness);
		}

        public ListGenome()
        {

        }

        public override void SetCrossoverPoint(int crossoverPoint)
		{
			CrossoverPoint = crossoverPoint;
		}

		public ListGenome(long length, object min, object max)
		{
			Length = length;
			TheMin = (int)min;
			TheMax = (int)max;
			for (int i = 0; i < Length; i++)
			{
			   int nextValue = (int)GenerateGeneValue(min, max);
			   TheArray.Add(nextValue);
			}
		}

		public override void Initialize()
		{

		}

		public override bool CanDie(float fitness)
		{
			if (CurrentFitness <= (int)(fitness * 100.0f))
			{
				return true;
			}

			return false;
		}

		public override bool CanReproduce(float fitness)
		{
			if (ListGenome.TheSeed.Next(100) >= (int)(fitness * 100.0f))
			{
				return true;
			}

			return false;
		}

		public override object GenerateGeneValue(object min, object max)
		{
			return TheSeed.Next((int)min, (int)max);
		}

		public override void Mutate()
		{
			MutationIndex = TheSeed.Next((int)Length);
			int val = (int)GenerateGeneValue(TheMin, TheMax);
			TheArray[MutationIndex] = val;

		}
        
		private float CalculateClosestRatioToPi()
		{
				double nFitness = (double)((int)TheArray[0])/(double)((int)TheArray[1]);
				nFitness = (double)Math.Abs(Math.PI - nFitness);
				nFitness = (float)Math.Abs( 1.0d/(float)nFitness);
		
			if (nFitness < 1.0f)
			{
				nFitness = .000000001f;
			}
			else
			{
			   nFitness = (float)Math.Log(nFitness)/100.0f;
			}

			return (float)nFitness;
		}
		
		private float CalculateClosestRatioToNaturalLog()
		{
			double nFitness = (double)((int)TheArray[0])/(double)((int)TheArray[1]);
			nFitness = (double)Math.Abs(Math.E - nFitness);
			nFitness = (float)Math.Abs( 1.0d/(float)nFitness);
		
			if (nFitness < 1.0f)
			{
				nFitness = .000000001f;
			}
			else
			{
				nFitness = (float)Math.Log(nFitness)/100.0f;
			}

			return (float)nFitness;
		}
        
		private float CalculateClosestProductSum()
		{
			float sum = 0.0f;
			float product = 1.0f;
			for (int i = 0; i < Length; i++)
			{
				sum += (int)TheArray[i];
				product *= (int)TheArray[i];
			}
			if (product == sum)
			{
				CurrentFitness = 1;
			}
			else
			{
				CurrentFitness = Math.Abs(Math.Abs(1.0f/(product - sum)) - 0.02f);
			}
			
			return CurrentFitness;
		}
        
		private float CalculateClosestSumTo10()
		{
			float sum = 0.0f;
			for (int i = 0; i < Length; i++)
			{
				sum += (int)TheArray[i];
			}

			if (sum == 10)
				return 1;
			else
			 return Math.Abs(Math.Abs(1.0f/(sum - 10)) - 0.02f);

		}
        
		private float CalculateNumbersProducingProductsWithSameDigitsAsFirst()
		{
			float product = 1.0f;
			for (int i = 0; i < Length; i++)
			{
				product *= (int)TheArray[i];
			}

			string strProduct = product.ToString();

			char stringMatchChar = strProduct[0];
			int count = 1;
			for (int i = 1; i < strProduct.Length; i++)
			{
				if  (strProduct[i] == stringMatchChar)
				{
					count++;
				}
			}

			return ((float)count/(float)Length);

		}

		public override float CalculateFitness()
		{
			CurrentFitness = CalculateClosestRatioToNaturalLog();
			return CurrentFitness;
		}

		public override string ToString()
		{
			string strResult = "";
			for (int i = 0; i < Length; i++)
			{
			  strResult = strResult + ((int)TheArray[i]).ToString() + " ";
			}

			strResult += "-->" + CurrentFitness.ToString();

			return strResult;
		}

		public int[] ToArray()
		{
			int[] result = new int[Length];
			for (int i = 0; i < Length; i++)
			{
				result[i] = ((int)TheArray[i]);
			}


			return result;
		}

		public override void CopyGeneInfo(Genome dest)
		{
			ListGenome theGene = (ListGenome)dest;
			theGene.Length = Length;
			theGene.TheMin = TheMin;
			theGene.TheMax = TheMax;
		}


		public override Genome Crossover(Genome g)
		{
			ListGenome aGene1 = new ListGenome();
			ListGenome aGene2 = new ListGenome();
			g.CopyGeneInfo(aGene1);
			g.CopyGeneInfo(aGene2);

			ListGenome CrossingGene = (ListGenome)g;
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
            
			ListGenome aGene = null;
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

	}
}
