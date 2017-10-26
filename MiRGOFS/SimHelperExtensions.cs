using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MiRGOFS
{
    public static class SimHelperExtensions
    {
        public static long CacheUseCount = 0;
        public static long CacheHitCount = 0;
        public static T2 GetOrSetSimCache<T, T2>(this Dictionary<T, Dictionary<T, T2>> cache, T go1, T go2, Func<T, T, T2> Calc)
        {
            if (cache.ContainsKey(go1) && cache[go1].ContainsKey(go2))
            {
                return cache[go1][go2];
            }
            var sim = Calc(go1, go2);
            try
            {
                if (!cache.ContainsKey(go1))
                {
                    cache[go1] = new Dictionary<T, T2>();
                }
                if (!cache.ContainsKey(go2))
                {
                    cache[go2] = new Dictionary<T, T2>();
                }
                cache[go1][go2] = sim;
                cache[go2][go1] = sim;
            }
            catch (Exception ex)
            { }

            return sim;
        }

        public static T2 GetOrSetSimCacheSingle<T, T2>(this Dictionary<T, Dictionary<T, T2>> cache, T go1, T go2, Func<T, T, T2> Calc)
        {
            if (cache.ContainsKey(go1) && cache[go1].ContainsKey(go2))
            {
                return cache[go1][go2];
            }
            var sim = Calc(go1, go2);
            try
            {
                if (!cache.ContainsKey(go1))
                {
                    cache[go1] = new Dictionary<T, T2>();
                }
                cache[go1][go2] = sim;
            }
            catch (Exception ex)
            { }

            return sim;
        }

        public static T2 GetOrSetSimCache<T2>(this Dictionary<int, T2>[] cache, int go1, int go2, Func<int, int, T2> Calc)
        {
            if (cache[go1].ContainsKey(go2))
            {
                return cache[go1][go2];
            }
            var sim = Calc(go1, go2);
            if (cache[go1] == null)
            {
                cache[go1] = new Dictionary<int, T2>();
            }
            if (cache[go2] == null)
            {
                cache[go2] = new Dictionary<int, T2>();
            }
            cache[go1][go2] = sim;
            cache[go2][go1] = sim;

            return sim;
        }

        public static float GetOrSetSimCache(this float[][] cache, int go1, int go2, Func<int, int, float> Calc)
        {
            //Interlocked.Increment(ref CacheUseCount);
            int tgo1 = TruncateGO(go1);
            if (cache[tgo1] == null)
            {
                Console.WriteLine("{0}, {1}", tgo1, go1);
            }
            int tgo2 = TruncateGO(go2);
            if (cache[tgo2] == null)
            {
                Console.WriteLine("{0}, {1}", tgo2, go2);
            }
            float rval = cache[tgo1][tgo2];
            if (rval > 0 && rval <= 100000)
            {
                //Interlocked.Increment(ref CacheHitCount);
                return rval;
            }

            var sim = Calc(go1, go2);
            cache[tgo1][tgo2] = sim;
            cache[tgo2][tgo1] = sim;
            return sim;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TruncateGO(this int go)
        {
            switch (go / 10000)
            {
                case 190:
                    return go - 1800000;
                case 199:
                case 200:
                    return go - 1880000;
                default:
                    return go;
            }
        }

        public static IEnumerable<int> IntersectSorted(this IEnumerable<int> sequence1, IEnumerable<int> sequence2)
        {
            using (var cursor1 = sequence1.GetEnumerator())
            using (var cursor2 = sequence2.GetEnumerator())
            {
                if (!cursor1.MoveNext() || !cursor2.MoveNext())
                {
                    yield break;
                }
                var value1 = cursor1.Current;
                var value2 = cursor2.Current;

                while (true)
                {
                    if (value1 < value2)
                    {
                        if (!cursor1.MoveNext())
                        {
                            yield break;
                        }
                        value1 = cursor1.Current;
                    }
                    else if (value1 > value2)
                    {
                        if (!cursor2.MoveNext())
                        {
                            yield break;
                        }
                        value2 = cursor2.Current;
                    }
                    else
                    {
                        yield return value1;
                        if (!cursor1.MoveNext() || !cursor2.MoveNext())
                        {
                            yield break;
                        }
                        value1 = cursor1.Current;
                        value2 = cursor2.Current;
                    }
                }
            }
        }

        public static List<int> IntersectSorted2(this List<int> firstSet, List<int> secondSet)
        {
            var firstCount = firstSet.Count;
            var secondCount = secondSet.Count;
            int firstIndex = 0, secondIndex = 0;
            var intersection = new List<int>();

            while (firstIndex < firstCount && secondIndex < secondCount)
            {
                var comp = firstSet[firstIndex].CompareTo(secondSet[secondIndex]);
                if (comp < 0)
                {
                    ++firstIndex;
                }
                else if (comp > 0)
                {
                    ++secondIndex;
                }
                else
                {
                    intersection.Add(firstSet[firstIndex]);
                    ++firstIndex;
                    ++secondIndex;
                }
            }

            return intersection;
        }
    }
}
