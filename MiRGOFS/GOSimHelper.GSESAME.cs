using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiRGOFS
{
    public partial class GOSimHelper
    {
        public float CalcGOSim_GSESAME(int go1, int go2)
        {
            int realgo1 = go1;
            int realgo2 = go2;
            if (_aliasMapping.ContainsKey(go1))
            {
                realgo1 = _aliasMapping[go1];
            }
            if (_aliasMapping.ContainsKey(go2))
            {
                realgo2 = _aliasMapping[go2];
            }
            return GOSimCache.GetOrSetSimCache(go1, go2, (g1, g2) => CalcGOSim_GSESAME_Internal(realgo1, realgo2));
            //return CalcGOSim_GSESAME_Internal(go1, go2);
        }

        private float CalcGOSim_GSESAME_Internal(int go1, int go2)
        {
            if (_dag[go1].Ancestors == null)
            {
                FindAncestors(go1);
            }
            if (_dag[go1].SVt == null)
            {
                _dag[go1].SVt = new Dictionary<int, float>();
            }
            if (_dag[go2].Ancestors == null)
            {
                FindAncestors(go2);
            }
            if (_dag[go2].SVt == null)
            {
                _dag[go2].SVt = new Dictionary<int, float>();
            }
            float numerator = 0;
            foreach (var an in _dag[go1].Ancestors)
            {
                if (an == go2 || _dag[go2].Ancestors.BinarySearch(an) >=0)
                {
                    numerator += (SValue(go1, an, _dag[go1].Ancestors) + SValue(go2, an, _dag[go2].Ancestors));
                }
            }
            if (_dag[go2].Ancestors.BinarySearch(go1) >= 0)
            {
                numerator += (SValue(go1, go1, _dag[go1].Ancestors) + SValue(go2, go1, _dag[go2].Ancestors));
            }
            //var sharedAncestors = Intersect(_dag[go1].Ancestors, _dag[go2].Ancestors);
            //if (_dag[go1].Ancestors.Contains(go2))
            //{
            //    sharedAncestors.Add(go2);
            //}
            //if (_dag[go2].Ancestors.Contains(go1))
            //{
            //    sharedAncestors.Add(go1);
            //}
            //var numerator = sharedAncestors.Select(t => SValue(go1, t, _dag[go1].Ancestors) + SValue(go2, t, _dag[go2].Ancestors)).Sum();
            var denominator = SValue(go1) + SValue(go2);
            return (float)(numerator / denominator);
        }

        private float SValue(int A, int t, List<int> TA)
        {
            if (A == t)
            {
                return 1;
            }
            else if (_dag[A].SVt.ContainsKey(t))
            {
                return _dag[A].SVt[t];
            }
            else
            {
                if (_dag[t].Children != null)
                {
                    float SAt = 0;
                    if (_dag[t].IsA_Reversed != null)
                    {
                        foreach (var t2 in _dag[t].IsA_Reversed)
                        {
                            float SAt2 = 0;
                            if (t2 == A)
                            {
                                SAt2 = 0.8F;
                            }
                            else if (TA.BinarySearch(t2) >= 0)
                            {
                                SAt2 = 0.8F * SValue(A, t2, TA);
                            }
                            if (SAt2 > SAt)
                            {
                                SAt = SAt2;
                            }
                        }
                    }
                    if (_dag[t].OtherRelations_Reversed != null)
                    {
                        foreach (var t2 in _dag[t].OtherRelations_Reversed)
                        {
                            float SAt2 = 0;
                            if (t2 == A)
                            {
                                SAt2 = 0.7F;
                            }
                            else if (TA.BinarySearch(t2) >= 0)
                            {
                                SAt2 = 0.7F * SValue(A, t2, TA);
                            }
                            if (SAt2 > SAt)
                            {
                                SAt = SAt2;
                            }
                        }
                    }
                    if (_dag[t].PartOf_Reversed != null)
                    {
                        foreach (var t2 in _dag[t].PartOf_Reversed)
                        {
                            float SAt2 = 0;
                            if (t2 == A)
                            {
                                SAt2 = 0.6F;
                            }
                            else if (TA.BinarySearch(t2) >= 0)
                            {
                                SAt2 = 0.6F * SValue(A, t2, TA);
                            }
                            if (SAt2 > SAt)
                            {
                                SAt = SAt2;
                            }
                        }
                    }
                    _dag[A].SVt[t] = SAt;
                    return SAt;
                }
                throw new Exception("t does not have any children, bad data");
            }
        }

        private float SValue(int A)
        {
            if (_dag[A].SV <= 0)
            {
                if (_dag[A].Ancestors == null)
                {
                    FindAncestors(A);
                }
                _dag[A].SV = _dag[A].Ancestors.Select(t => SValue(A, t, _dag[A].Ancestors)).Sum() + 1; // 1 == SValue(A, A)
            }
            return _dag[A].SV;
        }
    }
}
