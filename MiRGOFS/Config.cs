using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiRGOFS
{
    public static class Config
    {
        public static int DAGCount = 3;
        public static string RNASimCacheFile = "simCache.json";
        public static string DAGPathPattern = @"G:\SS\CommonAD\DAG_new{0}.txt";
        public static string Gene2GOPath = @"G:\SS\CommonAD\gene2go.json";
        public static string MiRNAPath = @"G:\SS\CommonAD\miranda";
        public static string GeneralTablePath = @"G:\SS\CommonAD\Go_annotated_times.txt";
    }
}
