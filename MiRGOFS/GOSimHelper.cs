using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MiRGOFS.Model;

namespace MiRGOFS
{
    public partial class GOSimHelper
    {
        byte[] _bitmap;
        DAGNode[] _dag;
        int[] _allGO;
        //Dictionary<int, Dictionary<int, double>> _goSimCache;
        public Dictionary<int, int> _aliasMapping;
        double _base;

        static float[][] GOSimCache;
        static int MaxGOTruncated;
        //static GOSimHelper()
        //{
        //    GOSimCache_GSESAME = new float[MaxGO + 1][];
        //}

        public DAGNode[] Dag
        {
            get
            {
                return _dag;
            }
        }

        private int _totalGenes;
        public int TotalGenes { get
            {
                return _totalGenes;
            }
        }

        public static void InitGOSimCache(int[] allGOs)
        {
            Array.Sort(allGOs);
            if (allGOs != null && allGOs.Length > 0)
            {
                MaxGOTruncated = allGOs.Select(g => g.TruncateGO()).Max();
                if (GOSimCache == null)
                {
                    GOSimCache = new float[MaxGOTruncated + 1][];
                }
                foreach (var go in allGOs)
                {
                    int truncatedGO = go.TruncateGO();
                    if (GOSimCache[truncatedGO] == null)
                    {
                        GOSimCache[truncatedGO] = new float[MaxGOTruncated + 1];//new Dictionary<int, double>();
                        Array.Clear(GOSimCache[truncatedGO], 0, MaxGOTruncated + 1);
                    }
                }
            }
            Console.WriteLine("GOSimCache initialized, " + (MaxGOTruncated + 1));
        }

        public GOSimHelper(int dagId)
        {
            string dagFileName = string.Format(Config.DAGPathPattern, dagId);
            var dag = DAGLoader.LoadPreprocessedDAG(dagFileName);
            _totalGenes = dag.TotalGenes;
            _base = Math.Log(dag.TotalAnotedTimes);
            _allGO = dag.nodes.Keys.ToArray();
            _dag = new DAGNode[dag.nodes.Keys.Max() + 1];
            _aliasMapping = dag.AliasMapping;
            if (GOSimCache == null)
            {
                InitGOSimCache(_allGO);
            }
            foreach (var p in _aliasMapping)
            {
                var truncatedGO = p.Value.TruncateGO();
                if (GOSimCache[p.Value.TruncateGO()] == null)
                {
                    GOSimCache[truncatedGO] = new float[MaxGOTruncated + 1];//new Dictionary<int, double>();
                    Array.Clear(GOSimCache[truncatedGO], 0, MaxGOTruncated + 1);
                }
            }
            foreach (var p in dag.nodes)
            {
                _dag[p.Key] = p.Value;
            }
            if (_aliasMapping!= null)
            {
                foreach(var pair in _aliasMapping)
                {
                    _dag[pair.Key] = _dag[pair.Value];
                }
            }
            //_goSimCache = new Dictionary<int, Dictionary<int, double>>();
            _bitmap = new byte[_allGO.Max() + 1];
            Array.Clear(_bitmap, 0, _bitmap.Length);
        }
        #region Test
        public void Test(string go1, string go2, bool GSESAME = false)
        {
            int id1 = int.Parse(go1.TrimStart('0'));
            int id2 = int.Parse(go2.TrimStart('0'));
            if (_dag[id1] == null && !_aliasMapping.ContainsKey(id1))
            {
                Console.WriteLine(go1 + " is not in dag.");
            }
            else if (_dag[id2] == null && !_aliasMapping.ContainsKey(id2))
            {
                Console.WriteLine(go2 + " is not in dag.");
            }
            else
            {
                Console.WriteLine(go1 + ", " + go2 + ": " + (GSESAME ? CalcGOSim_GSESAME(id1, id2) : CalcGOSim(id1, id2)));
            }
        }

        public void RandomTest(bool GSESAME = false)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var rand = new Random();

            for (int i = 0; i < 100000; i++)
            {
                var go1 = _allGO[rand.Next(_allGO.Length)];
                var go2 = _allGO[rand.Next(_allGO.Length)];
                //Console.WriteLine(go1 + ", " + go2 + ": " + CalcSim(go1,go2));
                if (GSESAME)
                {
                    CalcGOSim_GSESAME(go1, go2);
                }
                else
                {
                    CalcGOSim(go1, go2);
                }
            }
            watch.Stop();
            Console.WriteLine(watch.ElapsedMilliseconds);
        }
        #endregion Test

        public IEnumerable<int> AllGOs
        {
            get
            {
                return _dag.Where(n => n != null).Select(n => n.Id);
            }
        }

        public double Base
        {
            get
            {
                return _base;
            }
            set
            {
                _base = Math.Log(value);
            }
        }

        public List<int> FilterGOs(IEnumerable<int> gos)
        {
            return gos.Where(g => (g < _dag.Length && _dag[g] != null && _dag[g].P_an > 1) || _aliasMapping.ContainsKey(g)).ToList();
        }

        public double CalcGOSim(int go1, int go2)
        {
            if (_aliasMapping.ContainsKey(go1))
            {
                go1 = _aliasMapping[go1];
            }
            if (_aliasMapping.ContainsKey(go2))
            {
                go2 = _aliasMapping[go2];
            }
            return GOSimCache.GetOrSetSimCache(go1, go2, CalcGOSimInternal);
        }

        private float CalcGOSimInternal(int go1, int go2)
        {
            int pHxy = 0;
            int pLxy = 0;
            foreach (var node in FindHighestCommonDescendants(go1, go2))
            {
                pHxy += _dag[node].P_single;
            }
            var lca = FindLowestCommonAncestors(go1, go2);
            if (lca.Count == 1)//optimize, use pre-computed P_single instead of calculate again
            {
                pLxy = _dag[lca.First()].P_single - _dag[lca.First()].P_an;
            }
            else if (lca.Count == 2)
            {
                foreach (var i in _dag[lca[0]].Descendants.IntersectSorted2(_dag[lca[1]].Descendants))
                {
                    pLxy += _dag[i].P_an;
                }
            }
            else if (lca.Count > 2)
            {
                var intersect = _dag[lca[0]].Descendants.IntersectSorted2(_dag[lca[1]].Descendants);
                for (int i = 2; i < lca.Count; i++)
                {
                    intersect = intersect.IntersectSorted2(_dag[lca[i]].Descendants);
                }
                foreach (var i in intersect)
                {
                    pLxy += _dag[i].P_an;
                }
            }

            double p_x = Math.Log(_dag[go1].P_single) - _base;
            double p_y = Math.Log(_dag[go2].P_single) - _base;
            double p_i = Math.Log(pLxy) - _base;
            double p_u = Math.Log(pHxy) - _base;

            var sim = (p_i / p_u + p_i / p_x + p_i / p_y) * (0 - p_x - p_y) / 3;
            if (sim < 0 || double.IsNaN(sim))
            {
                sim = 0;
            }

            return (float)(sim);
        }

        private List<int> FindLowestCommonAncestors(int go1, int go2)
        {
            var ancestors1 = FindAncestors(go1);
            var ancestors2 = FindAncestors(go2);
            var commonNodes = ancestors1.IntersectSorted2(ancestors2);
            if (ancestors1.Contains(go2))
            {
                commonNodes.Add(go2);
            }
            if (ancestors2.Contains(go1))
            {
                commonNodes.Add(go1);
            }
            var lca = new List<int>();
            var commonBackup = commonNodes.ToArray();
            foreach (var node in commonBackup)
            {
                if (!commonNodes.Any(n => _dag[n].Ancestors.Contains(node)))
                {
                    lca.Add(node);
                }
                else
                {
                    commonNodes.Remove(node);
                }
            }
            foreach (var a in lca)
            {
                if (_dag[a].Descendants == null)
                {
                    _dag[a].Descendants = FindDescendants(a);
                }
            }
            return lca;
        }

        private List<int> FindHighestCommonDescendants(int go1, int go2)
        {
            var descendants1 = FindDescendants(go1);
            var descendants2 = FindDescendants(go2);
            var commonNodes = descendants1.IntersectSorted2(descendants2);
            if (descendants1.Contains(go2))
            {
                commonNodes.Add(go2);
            }
            if (descendants2.Contains(go1))
            {
                commonNodes.Add(go1);
            }
            var hcd = new List<int>();
            var commonBackup = commonNodes.ToArray();
            foreach (var node in commonBackup)
            {
                if (!commonNodes.Any(n => _dag[n].Descendants.Contains(node)))
                {
                    hcd.Add(node);
                }
                else
                {
                    commonNodes.Remove(node);
                }
            }

            return hcd;
        }

        private List<int> FindAncestors(int go)
        {
            if (_dag[go] == null || _dag[go].Parents == null)
            {
                Console.WriteLine(go);
            }
            if (_dag[go].Ancestors == null)
            {
                var ancestors = new List<int>();
                foreach (var p in _dag[go].Parents)
                {
                    ancestors.Add(p);
                    ancestors.AddRange(FindAncestors(p));
                }
                _dag[go].Ancestors = ancestors.Distinct().ToList();
                _dag[go].Ancestors.Sort();
            }
            return _dag[go].Ancestors;
        }

        private List<int> FindDescendants(int go)
        {
            if (_dag[go].Descendants == null)
            {
                var descendants = new List<int>();
                foreach (var c in _dag[go].Children)
                {
                    descendants.Add(c);
                    descendants.AddRange(FindDescendants(c));
                }
                _dag[go].Descendants = descendants.Distinct().ToList();
                _dag[go].Descendants.Sort();
            }
            return _dag[go].Descendants;
        }
    }
}
