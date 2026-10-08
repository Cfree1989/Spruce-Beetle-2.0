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
    /// Ranks horizontal through-dowels on packed axis-aligned boxes.
    /// A dowel is perpendicular to world Z and only runs through a widest-face normal
    /// that is itself horizontal. Empty spans between those pieces stay on the rod.
    /// </summary>
    internal static class ColumnDowels
    {
        internal const double Tolerance = 1e-6;

        internal struct Box
        {
            public double X0, Y0, Z0, X1, Y1, Z1;
        }

        internal sealed class Dowel
        {
            public int Axis;
            public double A0;
            public double A1;
            public double U;
            public double V;
            public int Score;
            public int[] Pieces;
        }

        internal sealed class Rank
        {
            public List<Dowel> Chosen = new List<Dowel>();
            public int Ranked;
            public int Collided;
            public int TieIns;
        }


        internal static Rank Select(IList<Box> boxes, int count, double diameter, double clearance)
        {
            var rank = new Rank();
            if (boxes == null || boxes.Count == 0 || diameter <= Tolerance)
                return rank;
            if (clearance < 0)
                clearance = 0;

            double hole = diameter + 2.0 * clearance;
            var xLines = new List<Dowel>();
            var yLines = new List<Dowel>();
            Collect(boxes, 0, hole, xLines);
            Collect(boxes, 1, hole, yLines);
            xLines.Sort(Compare);
            yLines.Sort(Compare);

            rank.Ranked = xLines.Count + yLines.Count;
            if (count < 1)
                return rank;

            TakeBothWays(xLines, yLines, count, hole, rank);
            AddTieIns(boxes, hole, rank);
            return rank;
        }


        static void TakeBothWays(List<Dowel> xLines, List<Dowel> yLines, int count, double hole, Rank rank)
        {
            int firstAxis = 0;
            if (yLines.Count > 0 && (xLines.Count == 0 || Compare(yLines[0], xLines[0]) < 0))
                firstAxis = 1;

            int[] cursor = { 0, 0 };
            var lists = new[] { xLines, yLines };
            int axis = firstAxis;
            int stalls = 0;
            while (rank.Chosen.Count < count && stalls < 2)
            {
                bool took = false;
                List<Dowel> list = lists[axis];
                while (cursor[axis] < list.Count)
                {
                    Dowel line = list[cursor[axis]];
                    cursor[axis]++;
                    if (Collides(line, rank.Chosen, hole))
                    {
                        rank.Collided++;
                        continue;
                    }

                    rank.Chosen.Add(line);
                    took = true;
                    break;
                }

                stalls = took ? 0 : stalls + 1;
                axis = 1 - axis;
            }
        }


        static void AddTieIns(IList<Box> boxes, double hole, Rank rank)
        {
            var pinned = new bool[boxes.Count];
            for (int i = 0; i < rank.Chosen.Count; i++)
            {
                int[] pieces = rank.Chosen[i].Pieces;
                for (int p = 0; p < pieces.Length; p++)
                    pinned[pieces[p]] = true;
            }

            for (int i = 0; i < boxes.Count; i++)
            {
                if (pinned[i])
                    continue;
                if (!TryTieIn(boxes, i, pinned, hole, rank.Chosen, out Dowel tie))
                    continue;

                rank.Chosen.Add(tie);
                rank.Ranked++;
                rank.TieIns++;
                for (int p = 0; p < tie.Pieces.Length; p++)
                    pinned[tie.Pieces[p]] = true;
            }
        }


        static bool TryTieIn(IList<Box> boxes, int looseIndex, bool[] pinned, double hole, List<Dowel> chosen, out Dowel tie)
        {
            tie = null;
            Box loose = boxes[looseIndex];
            Dowel best = null;
            bool bestShort = false;
            double bestGap = double.PositiveInfinity;

            for (int axis = 0; axis < 2; axis++)
            {
                if (!HasAxis(loose, axis))
                    continue;
                if (!TryTieOnAxis(boxes, looseIndex, axis, pinned, hole, chosen, out Dowel candidate, out bool shortSide, out double gap))
                    continue;
                if (best != null && bestShort && !shortSide)
                    continue;
                if (best != null && bestShort == shortSide && gap >= bestGap)
                    continue;

                best = candidate;
                bestShort = shortSide;
                bestGap = gap;
            }

            tie = best;
            return tie != null;
        }


        static bool TryTieOnAxis(
            IList<Box> boxes,
            int looseIndex,
            int axis,
            bool[] pinned,
            double hole,
            List<Dowel> chosen,
            out Dowel tie,
            out bool shortSide,
            out double gap)
        {
            tie = null;
            shortSide = false;
            gap = double.PositiveInfinity;
            Box loose = boxes[looseIndex];
            Rect looseFoot = Footprint(loose, axis);

            int stopIndex = -1;
            double stopGap = double.PositiveInfinity;
            bool stopShort = false;
            double centerU = 0;
            double centerV = 0;

            for (int i = 0; i < boxes.Count; i++)
            {
                if (!pinned[i] || i == looseIndex)
                    continue;

                Rect stopFoot = Footprint(boxes[i], axis);
                if (!OverlapCenter(looseFoot, stopFoot, hole, out double u, out double v))
                    continue;

                double faceGap = AxisGap(loose, boxes[i], axis);
                bool entryShort = IsShortSide(boxes[i], axis);
                bool better = stopIndex < 0
                    || (entryShort && !stopShort)
                    || (entryShort == stopShort && faceGap < stopGap);
                if (!better)
                    continue;

                stopIndex = i;
                stopGap = faceGap;
                stopShort = entryShort;
                centerU = u;
                centerV = v;
            }

            if (stopIndex < 0)
                return false;

            Box stop = boxes[stopIndex];
            bool looseIsPositive = Mid(loose, axis) >= Mid(stop, axis);
            double far = looseIsPositive ? Start(stop, axis) : End(stop, axis);
            double near = looseIsPositive ? End(stop, axis) : Start(stop, axis);

            var members = new List<int> { stopIndex, looseIndex };
            double outer = looseIsPositive ? End(loose, axis) : Start(loose, axis);
            for (int i = 0; i < boxes.Count; i++)
            {
                if (i == looseIndex || i == stopIndex || pinned[i] || !HasAxis(boxes[i], axis))
                    continue;

                Rect foot = Footprint(boxes[i], axis);
                if (!Holds(foot, centerU, centerV, hole))
                    continue;

                double mid = Mid(boxes[i], axis);
                if (looseIsPositive)
                {
                    if (mid < near - Tolerance)
                        continue;
                    double end = End(boxes[i], axis);
                    if (end > outer)
                        outer = end;
                }
                else
                {
                    if (mid > near + Tolerance)
                        continue;
                    double start = Start(boxes[i], axis);
                    if (start < outer)
                        outer = start;
                }

                members.Add(i);
            }

            double a0 = looseIsPositive ? far : outer;
            double a1 = looseIsPositive ? outer : far;
            if (a1 - a0 <= Tolerance)
                return false;

            var memberSet = new HashSet<int>(members);
            if (Blocked(boxes, axis, a0, a1, centerU, centerV, memberSet))
                return false;

            var pieces = members.ToArray();
            Array.Sort(pieces, (a, b) =>
            {
                int byStart = Start(boxes[a], axis).CompareTo(Start(boxes[b], axis));
                if (byStart != 0)
                    return byStart;
                return a.CompareTo(b);
            });

            tie = new Dowel
            {
                Axis = axis,
                A0 = a0,
                A1 = a1,
                U = centerU,
                V = centerV,
                Score = pieces.Length,
                Pieces = pieces
            };
            if (Collides(tie, chosen, hole))
            {
                tie = null;
                return false;
            }

            shortSide = stopShort;
            gap = stopGap;
            return true;
        }


        internal static List<string> Check()
        {
            var fails = new List<string>();
            CheckThree(fails);
            CheckGap(fails);
            CheckFlat(fails);
            CheckHole(fails);
            CheckClearance(fails);
            CheckTwoStacks(fails);
            CheckDifferentAxes(fails);
            CheckStud(fails);
            CheckFour(fails);
            CheckExtendingPair(fails);
            CheckCollision(fails);
            CheckBothWays(fails);
            CheckShortStop(fails);
            return fails;
        }


        static void Collect(IList<Box> boxes, int axis, double hole, List<Dowel> into)
        {
            var ids = new List<int>();
            for (int i = 0; i < boxes.Count; i++)
            {
                if (HasAxis(boxes[i], axis))
                    ids.Add(i);
            }

            if (ids.Count < 2)
                return;

            var uValues = new List<double>();
            var vValues = new List<double>();
            var rects = new Rect[ids.Count];
            for (int k = 0; k < ids.Count; k++)
            {
                rects[k] = Footprint(boxes[ids[k]], axis);
                uValues.Add(rects[k].U0);
                uValues.Add(rects[k].U1);
                vValues.Add(rects[k].V0);
                vValues.Add(rects[k].V1);
            }

            double[] us = Unique(uValues);
            double[] vs = Unique(vValues);
            int nu = us.Length - 1;
            int nv = vs.Length - 1;
            if (nu < 1 || nv < 1)
                return;

            var keys = new string[nv, nu];
            var covers = new List<int>[nv, nu];
            for (int r = 0; r < nv; r++)
            {
                for (int c = 0; c < nu; c++)
                {
                    double u0 = us[c];
                    double u1 = us[c + 1];
                    double v0 = vs[r];
                    double v1 = vs[r + 1];
                    if (u1 - u0 <= Tolerance || v1 - v0 <= Tolerance)
                    {
                        keys[r, c] = "";
                        continue;
                    }

                    var cover = new List<int>();
                    for (int k = 0; k < ids.Count; k++)
                    {
                        Rect rect = rects[k];
                        if (rect.U0 <= u0 + Tolerance && rect.U1 >= u1 - Tolerance
                            && rect.V0 <= v0 + Tolerance && rect.V1 >= v1 - Tolerance)
                            cover.Add(k);
                    }

                    covers[r, c] = cover;
                    keys[r, c] = cover.Count == 0 ? "" : string.Join(",", cover);
                }
            }

            var seen = new HashSet<string>();
            for (int r = 0; r < nv; r++)
            {
                for (int c = 0; c < nu; c++)
                {
                    string key = keys[r, c];
                    List<int> cover = covers[r, c];
                    if (cover == null || cover.Count < 2 || !seen.Add(key))
                        continue;
                    if (!TryCenter(keys, key, us, vs, hole, out double u, out double v))
                        continue;

                    var pieces = new int[cover.Count];
                    var member = new HashSet<int>();
                    for (int i = 0; i < cover.Count; i++)
                    {
                        pieces[i] = ids[cover[i]];
                        member.Add(pieces[i]);
                    }

                    Array.Sort(pieces, (a, b) =>
                    {
                        int byStart = Start(boxes[a], axis).CompareTo(Start(boxes[b], axis));
                        if (byStart != 0)
                            return byStart;
                        return a.CompareTo(b);
                    });

                    double a0 = double.PositiveInfinity;
                    double a1 = double.NegativeInfinity;
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        double s0 = Start(boxes[pieces[i]], axis);
                        double s1 = End(boxes[pieces[i]], axis);
                        if (s0 < a0)
                            a0 = s0;
                        if (s1 > a1)
                            a1 = s1;
                    }

                    if (a1 - a0 <= Tolerance)
                        continue;
                    if (Blocked(boxes, axis, a0, a1, u, v, member))
                        continue;

                    into.Add(new Dowel
                    {
                        Axis = axis,
                        A0 = a0,
                        A1 = a1,
                        U = u,
                        V = v,
                        Score = pieces.Length,
                        Pieces = pieces
                    });
                }
            }
        }


        static bool TryCenter(string[,] keys, string key, double[] us, double[] vs, double hole, out double u, out double v)
        {
            int nv = vs.Length - 1;
            int nu = us.Length - 1;
            double bestArea = -1;
            u = 0;
            v = 0;
            bool found = false;
            var open = new bool[nu];

            for (int r0 = 0; r0 < nv; r0++)
            {
                for (int c = 0; c < nu; c++)
                    open[c] = true;

                for (int r1 = r0; r1 < nv; r1++)
                {
                    for (int c = 0; c < nu; c++)
                    {
                        if (keys[r1, c] != key)
                            open[c] = false;
                    }

                    int c0 = 0;
                    while (c0 < nu)
                    {
                        while (c0 < nu && !open[c0])
                            c0++;
                        if (c0 >= nu)
                            break;

                        int c1 = c0;
                        while (c1 < nu && open[c1])
                            c1++;

                        double u0 = us[c0];
                        double u1 = us[c1];
                        double v0 = vs[r0];
                        double v1 = vs[r1 + 1];
                        double du = u1 - u0;
                        double dv = v1 - v0;
                        if (du + Tolerance >= hole && dv + Tolerance >= hole)
                        {
                            double area = du * dv;
                            if (area > bestArea)
                            {
                                bestArea = area;
                                u = (u0 + u1) * 0.5;
                                v = (v0 + v1) * 0.5;
                                found = true;
                            }
                        }

                        c0 = c1;
                    }
                }
            }

            return found;
        }


        static bool Blocked(IList<Box> boxes, int axis, double a0, double a1, double u, double v, HashSet<int> members)
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                if (members.Contains(i))
                    continue;

                Box box = boxes[i];
                double p0;
                double p1;
                double q0;
                double q1;
                double s0;
                double s1;
                if (axis == 0)
                {
                    p0 = box.Y0;
                    p1 = box.Y1;
                    q0 = box.Z0;
                    q1 = box.Z1;
                    s0 = box.X0;
                    s1 = box.X1;
                }
                else
                {
                    p0 = box.X0;
                    p1 = box.X1;
                    q0 = box.Z0;
                    q1 = box.Z1;
                    s0 = box.Y0;
                    s1 = box.Y1;
                }

                if (u <= p0 + Tolerance || u >= p1 - Tolerance)
                    continue;
                if (v <= q0 + Tolerance || v >= q1 - Tolerance)
                    continue;

                double lo = Math.Max(a0, s0);
                double hi = Math.Min(a1, s1);
                if (hi - lo > Tolerance)
                    return true;
            }

            return false;
        }


        static bool Collides(Dowel line, List<Dowel> chosen, double hole)
        {
            for (int i = 0; i < chosen.Count; i++)
            {
                if (Distance(line, chosen[i]) + Tolerance < hole)
                    return true;
            }

            return false;
        }


        static double Distance(Dowel a, Dowel b)
        {
            Ends(a, out double ax, out double ay, out double az, out double bx, out double by, out double bz);
            Ends(b, out double cx, out double cy, out double cz, out double dx, out double dy, out double dz);
            return SegmentDistance(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz);
        }


        internal static void Ends(Dowel line, out double x0, out double y0, out double z0, out double x1, out double y1, out double z1)
        {
            if (line.Axis == 0)
            {
                x0 = line.A0;
                y0 = line.U;
                z0 = line.V;
                x1 = line.A1;
                y1 = line.U;
                z1 = line.V;
                return;
            }

            x0 = line.U;
            y0 = line.A0;
            z0 = line.V;
            x1 = line.U;
            y1 = line.A1;
            z1 = line.V;
        }


        static double SegmentDistance(
            double ax, double ay, double az,
            double bx, double by, double bz,
            double cx, double cy, double cz,
            double dx, double dy, double dz)
        {
            double ux = bx - ax;
            double uy = by - ay;
            double uz = bz - az;
            double vx = dx - cx;
            double vy = dy - cy;
            double vz = dz - cz;
            double wx = ax - cx;
            double wy = ay - cy;
            double wz = az - cz;
            double aa = ux * ux + uy * uy + uz * uz;
            double bb = ux * vx + uy * vy + uz * vz;
            double cc = vx * vx + vy * vy + vz * vz;
            double dd = ux * wx + uy * wy + uz * wz;
            double ee = vx * wx + vy * wy + vz * wz;
            double disc = aa * cc - bb * bb;
            double sN;
            double sD = disc;
            double tN;
            double tD = disc;

            if (disc < Tolerance)
            {
                sN = 0.0;
                sD = 1.0;
                tN = ee;
                tD = cc;
            }
            else
            {
                sN = bb * ee - cc * dd;
                tN = aa * ee - bb * dd;
                if (sN < 0.0)
                {
                    sN = 0.0;
                    tN = ee;
                    tD = cc;
                }
                else if (sN > sD)
                {
                    sN = sD;
                    tN = ee + bb;
                    tD = cc;
                }
            }

            if (tN < 0.0)
            {
                tN = 0.0;
                if (-dd < 0.0)
                    sN = 0.0;
                else if (-dd > aa)
                    sN = sD;
                else
                {
                    sN = -dd;
                    sD = aa;
                }
            }
            else if (tN > tD)
            {
                tN = tD;
                if (-dd + bb < 0.0)
                    sN = 0.0;
                else if (-dd + bb > aa)
                    sN = sD;
                else
                {
                    sN = -dd + bb;
                    sD = aa;
                }
            }

            double sc = Math.Abs(sN) < Tolerance || Math.Abs(sD) < Tolerance ? 0.0 : sN / sD;
            double tc = Math.Abs(tN) < Tolerance || Math.Abs(tD) < Tolerance ? 0.0 : tN / tD;
            double rx = wx + sc * ux - tc * vx;
            double ry = wy + sc * uy - tc * vy;
            double rz = wz + sc * uz - tc * vz;
            return Math.Sqrt(rx * rx + ry * ry + rz * rz);
        }


        static int Compare(Dowel a, Dowel b)
        {
            int byScore = b.Score.CompareTo(a.Score);
            if (byScore != 0)
                return byScore;
            int byAxis = a.Axis.CompareTo(b.Axis);
            if (byAxis != 0)
                return byAxis;
            int byStart = a.A0.CompareTo(b.A0);
            if (byStart != 0)
                return byStart;
            int byU = a.U.CompareTo(b.U);
            if (byU != 0)
                return byU;
            return a.V.CompareTo(b.V);
        }


        static bool HasAxis(Box box, int axis)
        {
            double dx = box.X1 - box.X0;
            double dy = box.Y1 - box.Y0;
            double dz = box.Z1 - box.Z0;
            if (dx <= Tolerance || dy <= Tolerance || dz <= Tolerance)
                return false;

            double areaX = dy * dz;
            double areaY = dx * dz;
            double areaZ = dx * dy;
            double widest = areaX;
            if (areaY > widest)
                widest = areaY;
            if (areaZ > widest)
                widest = areaZ;

            double tie = Math.Max(Tolerance, widest * 1e-9);
            if (axis == 0)
                return areaX + tie >= widest;
            return areaY + tie >= widest;
        }


        struct Rect
        {
            public double U0, V0, U1, V1;
        }


        static Rect Footprint(Box box, int axis)
        {
            if (axis == 0)
            {
                return new Rect { U0 = box.Y0, V0 = box.Z0, U1 = box.Y1, V1 = box.Z1 };
            }

            return new Rect { U0 = box.X0, V0 = box.Z0, U1 = box.X1, V1 = box.Z1 };
        }


        static double Mid(Box box, int axis)
        {
            return (Start(box, axis) + End(box, axis)) * 0.5;
        }


        static double AxisGap(Box loose, Box stop, int axis)
        {
            double l0 = Start(loose, axis);
            double l1 = End(loose, axis);
            double s0 = Start(stop, axis);
            double s1 = End(stop, axis);
            if (l1 <= s0 + Tolerance)
                return s0 - l1;
            if (s1 <= l0 + Tolerance)
                return l0 - s1;
            return 0;
        }


        static bool IsShortSide(Box box, int axis)
        {
            double dx = box.X1 - box.X0;
            double dy = box.Y1 - box.Y0;
            double dz = box.Z1 - box.Z0;
            double len = axis == 0 ? dx : dy;
            double min = Math.Min(dx, Math.Min(dy, dz));
            double max = Math.Max(dx, Math.Max(dy, dz));
            return len > min + Tolerance && len < max - Tolerance;
        }


        static bool OverlapCenter(Rect a, Rect b, double hole, out double u, out double v)
        {
            u = 0;
            v = 0;
            double u0 = Math.Max(a.U0, b.U0);
            double u1 = Math.Min(a.U1, b.U1);
            double v0 = Math.Max(a.V0, b.V0);
            double v1 = Math.Min(a.V1, b.V1);
            if (u1 - u0 + Tolerance < hole || v1 - v0 + Tolerance < hole)
                return false;

            u = (u0 + u1) * 0.5;
            v = (v0 + v1) * 0.5;
            return true;
        }


        static bool Holds(Rect rect, double u, double v, double hole)
        {
            double margin = hole * 0.5;
            return u >= rect.U0 + margin - Tolerance
                && u <= rect.U1 - margin + Tolerance
                && v >= rect.V0 + margin - Tolerance
                && v <= rect.V1 - margin + Tolerance;
        }


        static double Start(Box box, int axis)
        {
            return axis == 0 ? box.X0 : box.Y0;
        }


        static double End(Box box, int axis)
        {
            return axis == 0 ? box.X1 : box.Y1;
        }


        static double[] Unique(List<double> values)
        {
            values.Sort();
            var unique = new List<double>();
            for (int i = 0; i < values.Count; i++)
            {
                if (unique.Count == 0 || values[i] - unique[unique.Count - 1] > Tolerance)
                    unique.Add(values[i]);
            }

            return unique.ToArray();
        }


        static Box Slab(double x0, double x1, double y0, double y1, double z0, double z1)
        {
            return new Box { X0 = x0, Y0 = y0, Z0 = z0, X1 = x1, Y1 = y1, Z1 = z1 };
        }


        static void CheckThree(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 4),
                Slab(1, 2, 0, 4, 0, 4),
                Slab(2, 3, 0, 4, 0, 4)
            };
            Rank rank = Select(boxes, 4, 0.5, 0);
            Expect(fails, "three", rank.Ranked == 1 && rank.Chosen.Count == 1, "expected one line");
            if (rank.Chosen.Count != 1)
                return;

            Dowel line = rank.Chosen[0];
            Expect(fails, "three", line.Axis == 0 && line.Score == 3 && line.Pieces.Length == 3, "score 3 through all three");
            Expect(fails, "three", Near(line.A0, 0) && Near(line.A1, 3), "span 0 to 3");
            Expect(fails, "three", Near(line.U, 2) && Near(line.V, 2), "center of the footprint");
            Expect(fails, "three", line.Pieces[0] == 0 && line.Pieces[1] == 1 && line.Pieces[2] == 2, "piece order");
        }


        static void CheckGap(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 4),
                Slab(1, 2, 0, 4, 0, 4),
                Slab(4, 5, 0, 4, 0, 4)
            };
            Rank rank = Select(boxes, 4, 0.5, 0);
            Expect(fails, "gap", rank.Chosen.Count == 1, "still one line");
            if (rank.Chosen.Count != 1)
                return;

            Dowel line = rank.Chosen[0];
            Expect(fails, "gap", line.Score == 3 && Near(line.A0, 0) && Near(line.A1, 5), "span crosses the empty section");
        }


        static void CheckFlat(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 10, 0, 10, 0, 1),
                Slab(0, 10, 0, 10, 1, 2),
                Slab(0, 10, 0, 10, 2, 3)
            };
            Rank rank = Select(boxes, 4, 0.5, 0);
            Expect(fails, "flat", rank.Ranked == 0, "thickness on Z is not a dowel");
        }


        static void CheckHole(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 4),
                Slab(1, 2, 0, 4, 0, 4),
                Slab(2, 3, 0, 4, 0, 4)
            };
            Rank fit = Select(boxes, 4, 4, 0);
            Rank miss = Select(boxes, 4, 4.1, 0);
            Expect(fails, "hole", fit.Chosen.Count == 1, "hole equal to the face still fits");
            Expect(fails, "hole", miss.Ranked == 0, "hole larger than the face is dropped");
        }


        static void CheckClearance(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 0.5, 0, 1, 0, 4),
                Slab(0.5, 1, 0, 1, 0, 4)
            };
            Rank loose = Select(boxes, 4, 0.99, 0);
            Rank tight = Select(boxes, 4, 0.99, 0.01);
            Expect(fails, "clearance", loose.Chosen.Count == 1, "fits when clearance is 0");
            Expect(fails, "clearance", tight.Ranked == 0, "hole Dia+2*Cl larger than the face is dropped");
        }


        static void CheckTwoStacks(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 4),
                Slab(1, 2, 0, 4, 0, 4),
                Slab(0, 1, 20, 24, 0, 4),
                Slab(1, 2, 20, 24, 0, 4)
            };
            Rank one = Select(boxes, 1, 0.5, 0);
            Rank both = Select(boxes, 2, 0.5, 0);
            Expect(fails, "stacks", one.Ranked == 2 && one.Chosen.Count == 1, "N = 1 returns one of two lines");
            Expect(fails, "stacks", both.Chosen.Count == 2, "N = 2 returns both");
            if (one.Chosen.Count == 1)
                Expect(fails, "stacks", one.Chosen[0].Pieces[0] == 0, "tie keeps the lower footprint");
        }


        static void CheckDifferentAxes(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1.5, 0, 3.5, 0, 20),
                Slab(1.5, 5.0, 0, 1.5, 0, 20)
            };
            Rank rank = Select(boxes, 4, 0.5, 0);
            Expect(fails, "axes", rank.Ranked == 0, "different thicknesses do not share a dowel");
        }


        static void CheckStud(List<string> fails)
        {
            var edge = new List<Box>
            {
                Slab(0, 1.5, 0, 3.5, 0, 20),
                Slab(0, 1.5, 3.5, 7.0, 0, 20)
            };
            var face = new List<Box>
            {
                Slab(0, 1.5, 0, 3.5, 0, 20),
                Slab(1.5, 3.0, 0, 3.5, 0, 20)
            };
            Rank edgeRank = Select(edge, 4, 0.5, 0);
            Rank faceRank = Select(face, 4, 0.5, 0);
            Expect(fails, "stud-edge", edgeRank.Ranked == 0, "narrow-face touch is not a dowel");
            Expect(fails, "stud-face", faceRank.Chosen.Count == 1 && faceRank.Chosen[0].Axis == 0 && faceRank.Chosen[0].Score == 2,
                "wide-face touch is one horizontal dowel through the 1.5 thickness");
        }


        static void CheckFour(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 4),
                Slab(1, 2, 0, 4, 0, 4),
                Slab(2, 3, 0, 4, 0, 4),
                Slab(3, 4, 0, 4, 0, 4)
            };
            Rank rank = Select(boxes, 8, 0.5, 0);
            Expect(fails, "four", rank.Ranked == 1 && rank.Chosen.Count == 1 && rank.Chosen[0].Score == 4,
                "one line, score 4, pairs inside the footprint are not returned");
        }


        static void CheckExtendingPair(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 6, 0, 4),
                Slab(1, 2, 0, 6, 0, 4),
                Slab(2, 3, 0, 4, 0, 4),
                Slab(3, 4, 0, 4, 0, 4)
            };
            Rank rank = Select(boxes, 8, 0.5, 0);
            Expect(fails, "extend", rank.Ranked == 2, "the overhanging pair is its own line");
            if (rank.Chosen.Count < 2)
                return;

            Expect(fails, "extend", rank.Chosen[0].Score == 4 && rank.Chosen[1].Score == 2, "longer line ranks first");
            Expect(fails, "extend", rank.Chosen[1].Pieces.Length == 2 && Near(rank.Chosen[1].U, 5), "pair center sits in the overhang");
        }


        static void CheckCollision(List<string> fails)
        {
            // X-dowel along X = 0..2 at (Y=2, Z=2). Y-dowel along Y = 0..2 at (X=5, Z=2).
            // Centerline gap is 3. A 3.5 hole skips the second line; a 0.5 hole keeps both.
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 4),
                Slab(1, 2, 0, 4, 0, 4),
                Slab(3, 7, 0, 1, 0, 4),
                Slab(3, 7, 1, 2, 0, 4)
            };
            Rank close = Select(boxes, 2, 3.5, 0);
            Rank apart = Select(boxes, 2, 0.5, 0);
            Expect(fails, "collision", close.Ranked == 2 && close.Chosen.Count == 1 && close.Collided == 1,
                "a line closer than one hole-diameter is skipped");
            Expect(fails, "collision", apart.Chosen.Count == 2 && apart.Collided == 0, "a wider gap keeps both");
        }


        static void CheckBothWays(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 4),
                Slab(1, 2, 0, 4, 0, 4),
                Slab(2, 3, 0, 4, 0, 4),
                Slab(10, 14, 0, 1, 20, 24),
                Slab(10, 14, 1, 2, 20, 24)
            };
            Rank rank = Select(boxes, 2, 0.5, 0);
            Expect(fails, "both-ways", rank.Chosen.Count == 2, "N = 2 keeps one line on each axis");
            if (rank.Chosen.Count < 2)
                return;

            Expect(fails, "both-ways", rank.Chosen[0].Axis != rank.Chosen[1].Axis, "the two dowels are not the same direction");
            Expect(fails, "both-ways", rank.TieIns == 0, "both lines are full runs, not tie-ins");
        }


        static void CheckShortStop(List<string> fails)
        {
            var boxes = new List<Box>
            {
                Slab(0, 1, 0, 4, 0, 8),
                Slab(1, 2, 0, 4, 0, 8),
                Slab(1.1, 1.9, 4, 4.6, 6, 8),
                Slab(1, 2, -2, 0, 0, 8)
            };
            Rank rank = Select(boxes, 1, 0.5, 0);
            Dowel tie = null;
            for (int i = 0; i < rank.Chosen.Count; i++)
            {
                if (rank.Chosen[i].Axis == 1)
                    tie = rank.Chosen[i];
            }

            Expect(fails, "short-stop", rank.TieIns == 1 && tie != null, "the loose piece gets a tie-in");
            if (tie == null)
                return;

            Expect(fails, "short-stop", tie.Pieces.Length == 2 && tie.Pieces[0] == 1 && tie.Pieces[1] == 2,
                "the dowel is the loose piece plus the pinned piece it meets");
            Expect(fails, "short-stop", Near(tie.A0, 0) && Near(tie.A1, 4.6),
                "it stops at the far short side of that piece and does not continue through the column");
        }


        static void Expect(List<string> fails, string name, bool ok, string message)
        {
            if (!ok)
                fails.Add(name + ": " + message);
        }


        static bool Near(double a, double b)
        {
            return Math.Abs(a - b) <= 1e-6;
        }
    }
}
