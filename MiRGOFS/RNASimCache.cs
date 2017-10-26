using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using MiRGOFS.Model;

namespace MiRGOFS
{
    public static class RNASimCache
    {
        public static Dictionary<string, Dictionary<string, double[]>> Dict = new Dictionary<string, Dictionary<string, double[]>>();
        public static Dictionary<string, Dictionary<string, double[]>> Read(string fileName)
        {
            if (File.Exists(fileName))
            {
                var content = File.ReadAllText(fileName);
                try
                {
                    Dict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double[]>>>(content);
                }
                catch (Exception ex)
                {
                    DeserializeSimContent(content);
                }
            }
            return Dict;
        }

        private static void DeserializeSimContent(string content)
        {
            var sims = JsonConvert.DeserializeObject<List<MiRNASim>>(content);
            Dict = new Dictionary<string, Dictionary<string, double[]>>();
            foreach (var sim in sims)
            {
                if (!Dict.ContainsKey(sim.RNA1))
                {
                    Dict[sim.RNA1] = new Dictionary<string, double[]>();
                }
                if (!Dict.ContainsKey(sim.RNA2))
                {
                    Dict[sim.RNA2] = new Dictionary<string, double[]>();
                }
                Dict[sim.RNA1][sim.RNA2] = sim.Sim;
                Dict[sim.RNA2][sim.RNA1] = sim.Sim;
            }
        }

        public static void Save(string fileName)
        {
            try
            {
                File.WriteAllText(fileName, JsonConvert.SerializeObject(Dict));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
