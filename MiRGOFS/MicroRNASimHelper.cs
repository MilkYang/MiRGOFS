using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MiRGOFS.Model;

namespace MiRGOFS
{
    public class MicroRNASimHelper
    {
        public enum Mode
        {
            Count,
            Euclidean,
            Hyper,
            Max
        }

        IReadOnlyDictionary<string, MiRNA> _miRNAs;
        Dictionary<string, Dictionary<string, double>> _miRNASimCache;
        GOSimHelper[] _goSimHelpers;
        List<MiRNASim> _jobs;
        bool _useGSESAME;
        Mode _mode;
        public MicroRNASimHelper(IReadOnlyDictionary<string, MiRNA> miRNAs = null, bool useGSESAME = false, Mode mode = Mode.Count)
        {
            _miRNAs = miRNAs ?? DAGLoader.LoadMicroRNA2(Config.MiRNAPath);
            _miRNASimCache = new Dictionary<string, Dictionary<string, double>>();
            _goSimHelpers = Enumerable.Range(1, Config.DAGCount).Select(i => new GOSimHelper(i)).ToArray();
            _jobs = new List<MiRNASim>();
            _useGSESAME = useGSESAME;
            _mode = mode;
        }

        public string[] AllRNAs
        {
            get
            {
                return _miRNAs.Keys.ToArray();
            }
        }

        public void AddJob(string rna1, string rna2)
        {
            _jobs.Add(new MiRNASim()
            {
                RNA1 = rna1,
                RNA2 = rna2
            });
        }

        public List<MiRNASim> CalcAllJobs()
        {
            for (int i = 0; i < _jobs.Count; i++)
            {
                var job = _jobs[i];
                job.Sim = CalcSim(job.RNA1, job.RNA2);
                //RNASimCache.Save(Config.RNASimCacheFile);
                Console.WriteLine("[{0}]: {1}", DateTime.Now, job);
                Console.WriteLine("[{0}]: {1}/{2}", DateTime.Now, i, _jobs.Count);
            }
            return _jobs;
        }

        public Task<List<MiRNASim>> CalcAllJobsAsync()
        {
            return Task.Run(() =>
            {
                return CalcAllJobs();
            });
        }

        static double[] defaultSim = new double[] { 0,0,0};

        public double[] CalcSim(string rna1, string rna2)
        {
            if (_mode == Mode.Euclidean)
            {
                return RNASimCache.Dict.GetOrSetSimCacheSingle(rna1, rna2, (r1, r2) => {
                    if (!_miRNAs.ContainsKey(r1) || !_miRNAs.ContainsKey(r2))
                    {
                        return defaultSim;
                    }
                    return _goSimHelpers.Select(goSimHelper => CalcSimOnDAG_Euclidean(r1, r2, goSimHelper)).ToArray();
                });
            }
            return RNASimCache.Dict.GetOrSetSimCache(rna1, rna2, (r1, r2) => {
                if (!_miRNAs.ContainsKey(r1) || !_miRNAs.ContainsKey(r2))
                {
                    return defaultSim;
                }
                return _goSimHelpers.Select(goSimHelper => {
                    if (_mode == Mode.Hyper || _mode == Mode.Count)
                    {
                        return CalcSimOnDAG_HyperOrCount(r1, r2, goSimHelper);
                    }
                    else
                    {
                        return CalcSimOnDAG_Max(r1, r2, goSimHelper);
                    }
                }).ToArray();
            });
        }

        private double CalcSimOnDAG_Max(string rna1, string rna2, GOSimHelper goSimHelper)
        {
            double sim = 0;
            var goSet1 = _miRNAs[rna1].GOs;
            var goSet2 = _miRNAs[rna2].GOs;
            var filteredGOSet1 = goSimHelper.FilterGOs(goSet1);
            var filteredGOSet2 = goSimHelper.FilterGOs(goSet2);
            if (filteredGOSet1.Count == 0 || filteredGOSet2.Count == 0)
            {
                return 0.0;
            }
            foreach (var go in filteredGOSet1)
            {
                foreach (var go2 in filteredGOSet2)
                {
                    var sim2 = _useGSESAME ? goSimHelper.CalcGOSim_GSESAME(go, go2) : goSimHelper.CalcGOSim(go, go2);
                    if (sim < sim2)
                    {
                        sim = (float)sim2;
                    }
                }
            }
            return sim;
        }

        private double CalcSimOnDAG_HyperOrCount(string rna1, string rna2, GOSimHelper goSimHelper)
        {
            double sim = 0;
            int count = 0;
            var goSet1 = _miRNAs[rna1].GOs;
            var goSet2 = _miRNAs[rna2].GOs;
            var filteredGOSet1 = goSimHelper.FilterGOs(goSet1);
            var filteredGOSet2 = goSimHelper.FilterGOs(goSet2);
            if (filteredGOSet1.Count == 0 || filteredGOSet2.Count == 0)
            {
                return 0.0;
            }
            Dictionary<int, double> maxSimReverse = new Dictionary<int, double>();
            foreach (var go in filteredGOSet2)
            {
                maxSimReverse[go] = double.MinValue;
            }
            foreach (var go in filteredGOSet1)
            {
                double wx = 0;
                if (_mode == Mode.Hyper)
                {
                    wx = GetOrSetWeightCache(rna1, go, goSimHelper);
                }
                else
                {
                    wx = _miRNAs[rna1].kGOAnnotatedCount[go];
                    count += _miRNAs[rna1].kGOAnnotatedCount[go];
                }

                var maxSim = float.MinValue;
                foreach (var go2 in filteredGOSet2)
                {
                    var sim2 = _useGSESAME ? goSimHelper.CalcGOSim_GSESAME(go, go2) : goSimHelper.CalcGOSim(go, go2);
                    if (maxSim < sim2)
                    {
                        maxSim = (float)sim2;
                    }
                    if (maxSimReverse[go2] < sim2)
                    {
                        maxSimReverse[go2] = sim2;
                    }
                }
                sim += wx * maxSim;
            }
            foreach (var go in filteredGOSet2)
            {
                double wx = 0;
                if (_mode == Mode.Hyper)
                {
                    wx = GetOrSetWeightCache(rna2, go, goSimHelper);
                }
                else
                {
                    wx = _miRNAs[rna2].kGOAnnotatedCount[go];
                    count += _miRNAs[rna2].kGOAnnotatedCount[go];
                }

                sim += wx * maxSimReverse[go];
            }

            return sim / (_mode == Mode.Hyper ? (filteredGOSet1.Count + filteredGOSet2.Count) : count);
        }

        private double CalcSimOnDAG_Euclidean(string rna1, string rna2, GOSimHelper goSimHelper)
        {
            double sim = 0;
            var goSet1 = _miRNAs[rna1].GOs;
            var goSet2 = _miRNAs[rna2].GOs;
            var filteredGOSet1 = goSimHelper.FilterGOs(goSet1);
            var filteredGOSet2 = goSimHelper.FilterGOs(goSet2);
            foreach (var go in filteredGOSet1)
            {
                double wx = GetOrSetWeightCache(rna1, go, goSimHelper);
                double cSim = CalcGoGeneSim(go, filteredGOSet2, goSimHelper);
                sim += (wx * cSim * wx * cSim);
            }
            return Math.Sqrt(sim);
        }

        private float CalcGoGeneSim(int go, List<int> geneGOSet, GOSimHelper goSimHelper)
        {
            var maxSim = float.MinValue;
            foreach (var geneGO in geneGOSet)
            {
                var sim = _useGSESAME ? goSimHelper.CalcGOSim_GSESAME(go, geneGO) : goSimHelper.CalcGOSim(go, geneGO); //6355,8150 = Infinity?
                if (maxSim < sim)
                {
                    maxSim = (float)sim;
                }
            }
            return maxSim;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetOrSetWeightCache(string rna, int go, GOSimHelper goSimHelper)
        {
            double wx = 0;
            if (_miRNAs[rna].WeightCache.ContainsKey(go))
            {
                wx = _miRNAs[rna].WeightCache[go];
            }
            else
            {
                wx = HyperWeight.CalculateWeight(goSimHelper.TotalGenes, goSimHelper.Dag[go].P_an, _miRNAs[rna].nTargetGeneCount, _miRNAs[rna].kGOAnnotatedCount[go]);
                _miRNAs[rna].WeightCache[go] = wx;
            }

            return wx;
        }
    }
}
