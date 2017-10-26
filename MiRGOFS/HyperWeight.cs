using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;

namespace MiRGOFS
{
    public class HyperWeight
    {
        public static double CalculateWeight(int N, int M, int n, int k)
        {
            if (M < k)
            {
                return 0;
            }
            return Hypergeometric.CDF(N, M, n, k);
        }
    }
}
