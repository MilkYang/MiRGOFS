using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiRGOFS
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();

            //TestSingle();
            if (args != null && args.Length == 2)
            {
                bool useGSESAME = (args[0] == "GSESAME");
                MicroRNASimHelper.Mode mode = (MicroRNASimHelper.Mode)Enum.Parse(typeof(MicroRNASimHelper.Mode), args[1]);
                RunAllRNA(useGSESAME, mode);
            }

            Console.WriteLine("used seconds: " + watch.ElapsedMilliseconds / 1000.0);
        }

        public static void TestSingle()
        {
            Config.DAGPathPattern = @"DAG_new{0}.txt";
            Config.MiRNAPath = "miranda";

            var miRNAs = DAGLoader.LoadMicroRNA2(Config.MiRNAPath);
            GOSimHelper.InitGOSimCache(miRNAs.SelectMany(m => m.Value.GOs).Distinct().ToArray());
            MicroRNASimHelper rnaSim = new MicroRNASimHelper(miRNAs, false, MicroRNASimHelper.Mode.Euclidean);
            Stopwatch watch = new Stopwatch();
            watch.Start();

            //hsa-let-7c,hsa-let-7d-star
            Console.WriteLine(string.Join(", ", rnaSim.CalcSim("hsa-let-7c", "hsa-let-7d-star")));

            Console.WriteLine("used seconds: " + watch.ElapsedMilliseconds / 1000.0);
            Console.ReadKey();
        }

        public static void RunAllRNA(bool useGSESAME, MicroRNASimHelper.Mode mode, int start = 0, int end = 10000)
        {
            string simFileName = string.Format("sim_{0}_{1}", useGSESAME ? "GSESAME" : "Ours", mode.ToString());
            Config.DAGPathPattern = @"DAG_new{0}.txt";
            Config.MiRNAPath = "miranda";
            Config.GeneralTablePath = "Go_annotated_times.txt";
            Config.RNASimCacheFile = simFileName + "Cache.json";
            RNASimCache.Read(Config.RNASimCacheFile);
            var stateTimer = new Timer(obj => RNASimCache.Save(Config.RNASimCacheFile), null, 600000, 60000);
            int maxConcurrency = Math.Min(20, Environment.ProcessorCount);
            Console.WriteLine("Using {0} threads.", maxConcurrency);
            var miRNAs = DAGLoader.LoadMicroRNA2(Config.MiRNAPath);
            GOSimHelper.InitGOSimCache(miRNAs.SelectMany(m => m.Value.GOs).Distinct().ToArray());
            List<MicroRNASimHelper> helpers = new List<MicroRNASimHelper>();
            for (int i = 0; i < maxConcurrency; i++)
            {
                helpers.Add(new MicroRNASimHelper(miRNAs, useGSESAME, mode));
                Console.WriteLine("Helper {0} initialized.", i);
            }

            var allRNAs = helpers.First().AllRNAs;
            end = Math.Min(end, allRNAs.Length);
            for (int i = start; i < end; i++)
            {
                for (int j = mode == MicroRNASimHelper.Mode.Euclidean ? start : i; j < allRNAs.Length; j++)
                {
                    helpers[i % maxConcurrency].AddJob(allRNAs[i], allRNAs[j]);
                }
            }
            
            var tasks = helpers.Select(h => h.CalcAllJobsAsync()).ToArray();
            Task.WaitAll(tasks);
            var sims = tasks.SelectMany(t => t.Result).ToList();
            stateTimer.Dispose();
            File.WriteAllText(simFileName + ".json", JsonConvert.SerializeObject(sims));
        }
    }
}
