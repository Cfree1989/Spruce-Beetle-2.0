/*
 * MIT License
 *
 * Copyright (c) 2022 Dominik Reisach
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */


using System;
using System.Collections.Generic;


namespace SpruceBeetle.Packing
{
    /// <summary>
    /// Places axis-aligned offcuts inside a column so each piece contains vertical dowels.
    /// Coordinates are local to the column section: X/Y in plan, Z up the column.
    /// </summary>
    internal static class DowelColumn
    {
        internal const double Tolerance = 1e-4;

        // Vertices sit inside the inscribed circle so they clear the column faces.
        internal const double InsetScale = 0.75;

        internal const int MaxGeneratedCount = 32;

        internal struct Dowel
        {
            public double X;
            public double Y;
        }

        internal sealed class Placement
        {
            public int Source;
            public double X;
            public double Y;
            public double Z;
            public double Dx;
            public double Dy;
            public double Dz;
            public int[] Hits;
        }


        internal static int RequiredHits(int dowelCount)
        {
            if (dowelCount < 1)
                return 1;
            return Math.Min(2, dowelCount);
        }


        internal static List<Dowel> RegularPolygon(int count, double sizeX, double sizeY)
        {
            var dowels = new List<Dowel>();
            if (count < 1 || sizeX <= Tolerance || sizeY <= Tolerance)
                return dowels;

            double cx = sizeX * 0.5;
            double cy = sizeY * 0.5;
            if (count == 1)
            {
                dowels.Add(new Dowel { X = cx, Y = cy });
                return dowels;
            }

            double radius = 0.5 * Math.Min(sizeX, sizeY) * InsetScale;
            for (int i = 0; i < count; i++)
            {
                double angle = -Math.PI / 2.0 + i * (2.0 * Math.PI / count);
                dowels.Add(new Dowel
                {
                    X = cx + radius * Math.Cos(angle),
                    Y = cy + radius * Math.Sin(angle)
                });
            }

            return dowels;
        }


        internal static List<Placement> Place(
            IList<double> sizeX,
            IList<double> sizeY,
            IList<double> sizeZ,
            double boxX,
            double boxY,
            double boxZ,
            IList<Dowel> dowels,
            double lap)
        {
            var placed = new List<Placement>();
            if (sizeX == null || dowels == null || dowels.Count == 0)
                return placed;
            if (boxX <= Tolerance || boxY <= Tolerance || boxZ <= Tolerance)
                return placed;
            if (lap < 0)
                lap = 0;

            int n = sizeX.Count;
            int required = RequiredHits(dowels.Count);
            List<int[]> groups = Combinations(dowels.Count, required);

            var order = new List<int>(n);
            for (int i = 0; i < n; i++)
                order.Add(i);

            order.Sort((a, b) =>
            {
                double volA = sizeX[a] * sizeY[a] * sizeZ[a];
                double volB = sizeX[b] * sizeY[b] * sizeZ[b];
                int byVol = volB.CompareTo(volA);
                if (byVol != 0)
                    return byVol;

                double longA = Math.Max(sizeX[a], Math.Max(sizeY[a], sizeZ[a]));
                double longB = Math.Max(sizeX[b], Math.Max(sizeY[b], sizeZ[b]));
                int byLong = longB.CompareTo(longA);
                if (byLong != 0)
                    return byLong;

                return a.CompareTo(b);
            });

            for (int o = 0; o < order.Count; o++)
            {
                int source = order[o];
                Placement best = BestPlacement(
                    source,
                    sizeX[source],
                    sizeY[source],
                    sizeZ[source],
                    boxX,
                    boxY,
                    boxZ,
                    dowels,
                    groups,
                    required,
                    lap,
                    placed);

                if (best != null)
                    placed.Add(best);
            }

            return placed;
        }


        static Placement BestPlacement(
            int source,
            double dimX,
            double dimY,
            double dimZ,
            double boxX,
            double boxY,
            double boxZ,
            IList<Dowel> dowels,
            List<int[]> groups,
            int required,
            double lap,
            List<Placement> placed)
        {
            if (dimX <= Tolerance || dimY <= Tolerance || dimZ <= Tolerance)
                return null;

            Placement last = placed.Count > 0 ? placed[placed.Count - 1] : null;
            Placement best = null;
            double[] dims = { dimX, dimY, dimZ };
            var seen = new List<double[]>(6);

            for (int p = 0; p < 6; p++)
            {
                int ix = Permute(p, 0);
                int iy = Permute(p, 1);
                int iz = Permute(p, 2);
                double px = dims[ix];
                double py = dims[iy];
                double pz = dims[iz];

                if (px > boxX + Tolerance || py > boxY + Tolerance || pz > boxZ + Tolerance)
                    continue;
                if (AlreadyTried(seen, px, py, pz))
                    continue;

                seen.Add(new[] { px, py, pz });

                for (int g = 0; g < groups.Count; g++)
                {
                    if (!TryOrigin(groups[g], dowels, px, py, boxX, boxY, out double ox, out double oy))
                        continue;

                    int[] hits = DowelsInside(dowels, ox, oy, px, py);
                    if (hits.Length < required)
                        continue;

                    List<double> heights = CandidateZ(placed, pz, boxZ, lap);
                    for (int h = 0; h < heights.Count; h++)
                    {
                        double z = heights[h];
                        if (!Fits(ox, oy, z, px, py, pz, boxZ, hits, placed, lap))
                            continue;

                        var candidate = new Placement
                        {
                            Source = source,
                            X = ox,
                            Y = oy,
                            Z = z,
                            Dx = px,
                            Dy = py,
                            Dz = pz,
                            Hits = hits
                        };

                        if (best == null || IsBetter(candidate, best, last))
                            best = candidate;

                        break;
                    }
                }
            }

            return best;
        }


        // Six axis assignments of (dimX, dimY, dimZ) onto (plan X, plan Y, height).
        static int Permute(int perm, int axis)
        {
            switch (perm)
            {
                case 0: return axis == 0 ? 0 : axis == 1 ? 1 : 2;
                case 1: return axis == 0 ? 0 : axis == 1 ? 2 : 1;
                case 2: return axis == 0 ? 1 : axis == 1 ? 0 : 2;
                case 3: return axis == 0 ? 1 : axis == 1 ? 2 : 0;
                case 4: return axis == 0 ? 2 : axis == 1 ? 0 : 1;
                default: return axis == 0 ? 2 : axis == 1 ? 1 : 0;
            }
        }


        static bool AlreadyTried(List<double[]> seen, double px, double py, double pz)
        {
            for (int i = 0; i < seen.Count; i++)
            {
                if (Math.Abs(seen[i][0] - px) <= Tolerance
                    && Math.Abs(seen[i][1] - py) <= Tolerance
                    && Math.Abs(seen[i][2] - pz) <= Tolerance)
                    return true;
            }

            return false;
        }


        static bool TryOrigin(
            int[] group,
            IList<Dowel> dowels,
            double px,
            double py,
            double boxX,
            double boxY,
            out double ox,
            out double oy)
        {
            ox = 0;
            oy = 0;
            double ox0 = 0;
            double ox1 = boxX - px;
            double oy0 = 0;
            double oy1 = boxY - py;
            if (ox1 < -Tolerance || oy1 < -Tolerance)
                return false;

            for (int i = 0; i < group.Length; i++)
            {
                Dowel dowel = dowels[group[i]];
                ox0 = Math.Max(ox0, dowel.X - px);
                ox1 = Math.Min(ox1, dowel.X);
                oy0 = Math.Max(oy0, dowel.Y - py);
                oy1 = Math.Min(oy1, dowel.Y);
            }

            if (ox0 > ox1 + Tolerance || oy0 > oy1 + Tolerance)
                return false;

            ox = Clamp((ox0 + ox1) * 0.5, 0, Math.Max(0, boxX - px));
            oy = Clamp((oy0 + oy1) * 0.5, 0, Math.Max(0, boxY - py));
            return true;
        }


        static int[] DowelsInside(IList<Dowel> dowels, double ox, double oy, double px, double py)
        {
            var hits = new List<int>();
            for (int i = 0; i < dowels.Count; i++)
            {
                double x = dowels[i].X;
                double y = dowels[i].Y;
                if (x >= ox - Tolerance && x <= ox + px + Tolerance
                    && y >= oy - Tolerance && y <= oy + py + Tolerance)
                    hits.Add(i);
            }

            return hits.ToArray();
        }


        static List<double> CandidateZ(List<Placement> placed, double height, double boxZ, double lap)
        {
            var raw = new List<double> { 0 };
            for (int i = 0; i < placed.Count; i++)
            {
                double top = placed[i].Z + placed[i].Dz;
                raw.Add(top - lap);
                raw.Add(top);
                raw.Add(placed[i].Z - height);
                raw.Add(placed[i].Z + lap - height);
            }

            var heights = new List<double>();
            for (int i = 0; i < raw.Count; i++)
            {
                double z = raw[i];
                if (z < -Tolerance || z + height > boxZ + Tolerance)
                    continue;
                if (z < 0)
                    z = 0;
                if (z + height > boxZ)
                    z = boxZ - height;

                bool duplicate = false;
                for (int j = 0; j < heights.Count; j++)
                {
                    if (Math.Abs(heights[j] - z) <= Tolerance)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                    heights.Add(z);
            }

            heights.Sort();
            return heights;
        }


        // Shared dowels may occupy the same wood for at most `lap`.
        // Pieces that do not share a dowel may only touch.
        static bool Fits(
            double x,
            double y,
            double z,
            double dx,
            double dy,
            double dz,
            double boxZ,
            int[] hits,
            List<Placement> placed,
            double lap)
        {
            if (z < -Tolerance || z + dz > boxZ + Tolerance)
                return false;

            bool sharesAny = false;
            bool connected = false;

            for (int i = 0; i < placed.Count; i++)
            {
                Placement other = placed[i];
                bool share = Shares(hits, other.Hits);
                double xy = PlanOverlap(x, x + dx, y, y + dy, other.X, other.X + other.Dx, other.Y, other.Y + other.Dy);
                double zo = OverlapLength(z, z + dz, other.Z, other.Z + other.Dz);

                if (share)
                    sharesAny = true;
                if (share && zo >= lap - Tolerance)
                    connected = true;

                if (xy > Tolerance && zo > Tolerance)
                {
                    if (!share || zo > lap + Tolerance)
                        return false;
                }
            }

            if (sharesAny && !connected)
                return false;

            return true;
        }


        static bool IsBetter(Placement candidate, Placement best, Placement last)
        {
            if (candidate.Z < best.Z - Tolerance)
                return true;
            if (candidate.Z > best.Z + Tolerance)
                return false;

            if (last != null)
            {
                bool candidateShares = Shares(candidate.Hits, last.Hits);
                bool bestShares = Shares(best.Hits, last.Hits);
                if (candidateShares != bestShares)
                    return candidateShares;

                bool candidateOther = !SameHits(candidate.Hits, last.Hits);
                bool bestOther = !SameHits(best.Hits, last.Hits);
                if (candidateOther != bestOther)
                    return candidateOther;
            }

            if (candidate.Hits.Length != best.Hits.Length)
                return candidate.Hits.Length > best.Hits.Length;

            double candidateArea = candidate.Dx * candidate.Dy;
            double bestArea = best.Dx * best.Dy;
            return candidateArea > bestArea + Tolerance;
        }


        static bool Shares(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                for (int j = 0; j < b.Length; j++)
                {
                    if (a[i] == b[j])
                        return true;
                }
            }

            return false;
        }


        static bool SameHits(int[] a, int[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }


        static double PlanOverlap(
            double ax0, double ax1, double ay0, double ay1,
            double bx0, double bx1, double by0, double by1)
        {
            double ox = OverlapLength(ax0, ax1, bx0, bx1);
            double oy = OverlapLength(ay0, ay1, by0, by1);
            if (ox <= 0 || oy <= 0)
                return 0;
            return ox * oy;
        }


        static double OverlapLength(double a0, double a1, double b0, double b1)
        {
            return Math.Min(a1, b1) - Math.Max(a0, b0);
        }


        static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }


        static List<int[]> Combinations(int count, int take)
        {
            var results = new List<int[]>();
            if (take <= 0 || take > count)
                return results;

            var current = new int[take];
            Walk(count, take, 0, 0, current, results);
            return results;
        }


        static void Walk(int count, int take, int start, int depth, int[] current, List<int[]> results)
        {
            if (depth == take)
            {
                var copy = new int[take];
                Array.Copy(current, copy, take);
                results.Add(copy);
                return;
            }

            for (int i = start; i <= count - (take - depth); i++)
            {
                current[depth] = i;
                Walk(count, take, i + 1, depth + 1, current, results);
            }
        }
    }
}
