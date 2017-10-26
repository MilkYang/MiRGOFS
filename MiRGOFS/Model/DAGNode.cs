using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MiRGOFS.Model
{
    public class DAGNode
    {
        public int Id { get; set; }
        public List<int> Parents { get; set; }
        public List<int> IsA { get; set; }
        public List<int> PartOf { get; set; }
        public List<int> OtherRelations { get; set; }
        public List<int> Children { get; set; }
        public List<int> Alias { get; set; }

        [DefaultValue(1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int P_an { get; set; }

        [DefaultValue(1)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public int P_single { get; set; }

        [DefaultValue(0.0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public float SV { get; set; }

        [JsonIgnore]
        public List<int> IsA_Reversed { get; set; }
        [JsonIgnore]
        public List<int> PartOf_Reversed { get; set; }
        [JsonIgnore]
        public List<int> OtherRelations_Reversed { get; set; }
        [JsonIgnore]
        public List<int> Ancestors { get; set; }
        [JsonIgnore]
        public List<int> Descendants { get; set; }
        [JsonIgnore]
        public Dictionary<int, float> SVt { get; set; }
    }
}
