using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiRGOFS.Model
{
    public class GODAG
    {
        public Dictionary<int, DAGNode> nodes { get; set; }
        public Dictionary<int, int> AliasMapping { get; set; }
        public int TotalAnotedTimes { get; set; }
        public int TotalGenes { get; set; }

        [JsonIgnore]
        public int[] AllGos { get; set; }
        [JsonIgnore]
        public DAGNode[] NodeArray { get; set; }
    }
}
