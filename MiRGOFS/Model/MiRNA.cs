using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiRGOFS.Model
{
    public class MiRNA
    {
        public int nTargetGeneCount { get; set; }
        public int[] GOs { get; set; }
        public IReadOnlyDictionary<int, int> kGOAnnotatedCount { get; set; }
        public string[] TargetGenes { get; set; }
        public Dictionary<int, double> WeightCache { get; set; }
    }

    public class MiRNASim
    {
        public string RNA1 { get; set; }
        public string RNA2 { get; set; }
        public double[] Sim { get; set; }

        public override string ToString()
        {
            var simStr = Sim == null ? "NA" : string.Join(", ", Sim);
            return string.Format("{0},{1}: {2}", RNA1, RNA2, simStr);
        }
    }

    public class GeneralTable
    {
        public int N { get; set; }
        public IReadOnlyDictionary<int, int> M { get; set; }
    }
}
