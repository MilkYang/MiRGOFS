using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiRGOFS.Model;
using Newtonsoft.Json;

namespace MiRGOFS
{
    public class DAGLoader
    {
        public static IReadOnlyDictionary<int, DAGNode> LoadDAG(string DAGFile, string PSingleFile, string PanFile)
        {
            Dictionary<int, DAGNode> nodes = new Dictionary<int, DAGNode>();
            using (StreamReader srDAG = new StreamReader(new FileStream(DAGFile, FileMode.Open)))
            {
                using (StreamReader srPSingle = new StreamReader(new FileStream(PSingleFile, FileMode.Open)))
                {
                    using (StreamReader srPan = new StreamReader(new FileStream(PanFile, FileMode.Open)))
                    {
                        while (!srDAG.EndOfStream)
                        {
                            var line = srDAG.ReadLine();
                            var p_single = int.Parse(srPSingle.ReadLine().Trim());
                            var p_an = int.Parse(srPan.ReadLine().Trim());

                            List<int> parents = new List<int>();
                            List<int> children = new List<int>();
                            var n = (line.Length - 7) / 8;
                            if (n > 0)
                            {
                                for (int i = 0; i < n; i++)
                                {
                                    var id = int.Parse(line.Substring((i + 1) * 8, 7).TrimStart('0'));
                                    if (line[i * 8 + 7] == 'a')
                                    {
                                        parents.Add(id);
                                    }
                                    else
                                    {
                                        children.Add(id);
                                    }
                                }
                            }
                            var nodeId = int.Parse(line.Substring(0, 7).TrimStart('0'));
                            nodes.Add(nodeId, new DAGNode
                            {
                                Id = nodeId,
                                Parents = parents,
                                Children = children,
                                P_single = p_single,
                                P_an = p_an
                            });
                        }
                    }
                }
            }

            return nodes;
        }

        public static IReadOnlyDictionary<string, List<int>> LoadGene2GO(string gene2goFile)
        {
            using (StreamReader g2g = new StreamReader(new FileStream(gene2goFile, FileMode.Open)))
            {
                return JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(g2g.ReadToEnd());
            }
        }

        public static IReadOnlyDictionary<string, List<int>> LoadGene2GO2(string gene2goFile)
        {
            var lines = File.ReadAllLines(gene2goFile);
            Dictionary<string, List<int>> rval = new Dictionary<string, List<int>>();
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var parts = line.Trim().Split('\t');
                    if (parts.Length == 2)
                    {
                        rval.Add(parts[0], new List<int>());
                        int goCount = parts[1].Length / 7;
                        for (int i = 0; i < goCount; i++)
                        {
                            rval[parts[0]].Add(int.Parse(parts[1].Substring(7 * i, 7)));
                        }
                    }
                }
            }

            return rval;
        }

        public static IReadOnlyDictionary<string, MiRNA> LoadMiRNAGeneMapping(string mirnaFile)
        {
            var lines = File.ReadAllLines(mirnaFile);
            Dictionary<string, MiRNA> rval = new Dictionary<string, MiRNA>();
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var parts = line.Trim().Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        rval.Add(parts[0], new MiRNA() {
                            TargetGenes = parts.Skip(1).ToArray()
                        });
                    }
                }
            }

            return rval;
        }

        public static GODAG LoadPreprocessedDAG(string dagFile)
        {
            using (StreamReader srDag = new StreamReader(new FileStream(dagFile, FileMode.Open)))
            {
                var dag = JsonConvert.DeserializeObject<GODAG>(srDag.ReadToEnd());
                dag.AliasMapping = new Dictionary<int, int>();
                foreach (var pair in dag.nodes)
                {
                    if (pair.Value.Parents != null)
                    {
                        foreach(var p in pair.Value.Parents)
                        {
                            if (!dag.nodes.ContainsKey(p))
                            {
                                Console.WriteLine("Broken parent {0}", p);
                            }
                        }
                    }
                    if (pair.Value.Children != null)
                    {
                        foreach (var p in pair.Value.Children)
                        {
                            if (!dag.nodes.ContainsKey(p))
                            {
                                Console.WriteLine("Broken child {0}", p);
                            }
                        }
                    }
                    if (pair.Value.IsA != null && pair.Value.IsA.Count > 0)
                    {
                        foreach (var id in pair.Value.IsA)
                        {
                            if (dag.nodes[id].IsA_Reversed == null)
                            {
                                dag.nodes[id].IsA_Reversed = new List<int>();
                            }
                            dag.nodes[id].IsA_Reversed.Add(pair.Key);
                        }
                    }
                    if (pair.Value.PartOf != null && pair.Value.PartOf.Count > 0)
                    {
                        foreach (var id in pair.Value.PartOf)
                        {
                            if (dag.nodes[id].PartOf_Reversed == null)
                            {
                                dag.nodes[id].PartOf_Reversed = new List<int>();
                            }
                            dag.nodes[id].PartOf_Reversed.Add(pair.Key);
                        }
                    }
                    if (pair.Value.OtherRelations != null && pair.Value.OtherRelations.Count > 0)
                    {
                        foreach (var id in pair.Value.OtherRelations)
                        {
                            if (dag.nodes[id].OtherRelations_Reversed == null)
                            {
                                dag.nodes[id].OtherRelations_Reversed = new List<int>();
                            }
                            dag.nodes[id].OtherRelations_Reversed.Add(pair.Key);
                        }
                    }
                    if (pair.Value.Alias != null && pair.Value.Alias.Count > 0)
                    {
                        foreach (var id in pair.Value.Alias)
                        {
                            dag.AliasMapping.Add(id, pair.Key);
                        }
                    }
                }

                return dag;
            }
        }

        public static IReadOnlyDictionary<string, MiRNA> LoadMicroRNA(string path)
        {
            Dictionary<string, MiRNA> rnas = new Dictionary<string, MiRNA>();
            foreach (var file in Directory.EnumerateFiles(path, "*.txt"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                StreamReader srRNA = new StreamReader(new FileStream(file, FileMode.Open));
                string raw = srRNA.ReadToEnd();
                if (raw.Length % 7 != 0)
                {
                    throw new Exception("File format error: " + file);
                }

                var annotatedCount = new Dictionary<int, int>();
                for (int i = 0; i < raw.Length / 8; i++)
                {
                    int go = int.Parse(raw.Substring(i * 8, 8).TrimStart('0'));
                    if (!annotatedCount.ContainsKey(go))
                    {
                        annotatedCount.Add(go, 1);
                    }
                    else
                    {
                        annotatedCount[go] = annotatedCount[go] + 1;
                    }
                }
                rnas.Add(name, new MiRNA()
                {
                    kGOAnnotatedCount = annotatedCount,
                    GOs = annotatedCount.Keys.ToArray()
                });
            }

            return rnas;
        }

        public static IReadOnlyDictionary<string, MiRNA> LoadMicroRNA2(string path)
        {
            Dictionary<string, MiRNA> rnas = new Dictionary<string, MiRNA>();
            foreach (var file in Directory.EnumerateFiles(path, "*.txt"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                using (StreamReader srRNA = new StreamReader(new FileStream(file, FileMode.Open)))
                {
                    string firstLine = srRNA.ReadLine().Trim();
                    int targetGeneCount = 1;
                    if (int.TryParse(firstLine, out targetGeneCount))
                    {
                        srRNA.ReadLine();
                    }
                    else
                    {
                        srRNA.DiscardBufferedData();
                        srRNA.BaseStream.Seek(0, SeekOrigin.Begin);
                    }
                    var annotatedCount = new Dictionary<int, int>();
                    while (!srRNA.EndOfStream)
                    {
                        string raw = srRNA.ReadLine();
                        if (raw.Length % 7 != 0)
                        {
                            throw new Exception("File format error: " + file);
                        }
                        for (int i = 0; i < raw.Length / 7; i++)
                        {
                            int go = int.Parse(raw.Substring(i * 7, 7).TrimStart('0'));
                            if (!annotatedCount.ContainsKey(go))
                            {
                                annotatedCount.Add(go, 1);
                            }
                            else
                            {
                                annotatedCount[go] = annotatedCount[go] + 1;
                            }
                        }
                    }
                    rnas.Add(name, new MiRNA()
                    {
                        nTargetGeneCount = targetGeneCount,
                        kGOAnnotatedCount = annotatedCount,
                        GOs = annotatedCount.Keys.ToArray(),
                        WeightCache = new Dictionary<int, double>()
                    });
                }
            }

            return rnas;
        }

        public static GeneralTable LoadGeneralTable(string file)
        {
            GeneralTable rval = new GeneralTable();

            using (StreamReader srTable = new StreamReader(new FileStream(file, FileMode.Open)))
            {
                rval.N = int.Parse(srTable.ReadLine().Trim());

                var M = new Dictionary<int, int>();
                while (!srTable.EndOfStream)
                {
                    string line = srTable.ReadLine();
                    string[] parts = line.Split('\t');
                    if (parts.Length == 2)
                    {
                        M.Add(int.Parse(parts[0].TrimStart('0')), int.Parse(parts[1].Trim()));
                    }
                }
                rval.M = M;
            }
            return rval;
        }
    }
}
