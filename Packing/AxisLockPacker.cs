using System;
using System.Collections.Generic;
using CromulentBisgetti.ContainerPacking.Entities;

namespace SpruceBeetle.Packing
{
    /// <summary>
    /// Packs boxes with one side fixed on container height (world Z).
    /// Each piece sits on the floor or on a complete platform, so it does not bridge a hole.
    /// The other two sides may swap in plan. Stacks may still end at different heights.
    /// </summary>
    static class AxisLockPacker
    {
        const decimal Eps = 0.001m;

        public static List<Item> Pack(Container container, List<Item> items, bool longestOnZ)
        {
            var waiting = new List<Item>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                Item source = items[i];
                waiting.Add(new Item(source.ID, source.Dim1, source.Dim2, source.Dim3, 1));
            }

            waiting.Sort((a, b) =>
            {
                int cmp = Locked(b, longestOnZ).CompareTo(Locked(a, longestOnZ));
                if (cmp != 0)
                    return cmp;
                cmp = FootprintArea(b, longestOnZ).CompareTo(FootprintArea(a, longestOnZ));
                if (cmp != 0)
                    return cmp;
                return a.ID.CompareTo(b.ID);
            });

            var placed = new List<Item>();
            bool progress = true;
            while (progress)
            {
                progress = false;
                for (int i = 0; i < waiting.Count; i++)
                {
                    if (!TryPlace(container, placed, waiting[i], longestOnZ))
                        continue;
                    placed.Add(waiting[i]);
                    waiting.RemoveAt(i);
                    progress = true;
                    break;
                }
            }

            return placed;
        }


        static bool TryPlace(Container container, List<Item> placed, Item item, bool longestOnZ)
        {
            decimal height = Locked(item, longestOnZ);
            OtherSides(item, height, out decimal u, out decimal v);

            var xs = new List<decimal> { 0m };
            var zs = new List<decimal> { 0m };
            for (int i = 0; i < placed.Count; i++)
            {
                AddUnique(xs, placed[i].CoordX + placed[i].PackDimX);
                AddUnique(zs, placed[i].CoordZ + placed[i].PackDimZ);
            }

            bool found = false;
            decimal bestY = 0, bestX = 0, bestZ = 0, bestDx = 0, bestDz = 0;

            Consider(container, placed, xs, zs, u, v, height, ref found, ref bestY, ref bestX, ref bestZ, ref bestDx, ref bestDz);
            if (u != v)
                Consider(container, placed, xs, zs, v, u, height, ref found, ref bestY, ref bestX, ref bestZ, ref bestDx, ref bestDz);

            if (!found)
                return false;

            item.CoordX = bestX;
            item.CoordY = bestY;
            item.CoordZ = bestZ;
            item.PackDimX = bestDx;
            item.PackDimY = height;
            item.PackDimZ = bestDz;
            item.IsPacked = true;
            return true;
        }


        static void Consider(
            Container container,
            List<Item> placed,
            List<decimal> xs,
            List<decimal> zs,
            decimal dx,
            decimal dz,
            decimal height,
            ref bool found,
            ref decimal bestY,
            ref decimal bestX,
            ref decimal bestZ,
            ref decimal bestDx,
            ref decimal bestDz)
        {
            for (int ix = 0; ix < xs.Count; ix++)
            {
                decimal x = xs[ix];
                if (x + dx > container.Length + Eps)
                    continue;
                for (int iz = 0; iz < zs.Count; iz++)
                {
                    decimal z = zs[iz];
                    if (z + dz > container.Width + Eps)
                        continue;
                    if (!LowestSupported(placed, x, dx, z, dz, height, container.Height, out decimal y))
                        continue;
                    if (!found || y < bestY - Eps || (Abs(y - bestY) <= Eps && (x < bestX - Eps || (Abs(x - bestX) <= Eps && z < bestZ))))
                    {
                        found = true;
                        bestY = y;
                        bestX = x;
                        bestZ = z;
                        bestDx = dx;
                        bestDz = dz;
                    }
                }
            }
        }


        /// <summary>
        /// Lowest height where the piece fits and its whole bottom touches the floor or piece tops.
        /// </summary>
        static bool LowestSupported(List<Item> placed, decimal x, decimal dx, decimal z, decimal dz, decimal height, decimal limit, out decimal y)
        {
            var blocks = new List<decimal[]>(placed.Count);
            for (int i = 0; i < placed.Count; i++)
            {
                Item p = placed[i];
                if (!RangesOverlap(x, dx, p.CoordX, p.PackDimX) || !RangesOverlap(z, dz, p.CoordZ, p.PackDimZ))
                    continue;
                blocks.Add(new[] { p.CoordY, p.CoordY + p.PackDimY });
            }

            blocks.Sort((a, b) => a[0].CompareTo(b[0]));

            decimal cursor = 0m;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i][0] - cursor >= height - Eps && Supported(placed, x, dx, z, dz, cursor))
                {
                    y = cursor;
                    return true;
                }
                if (blocks[i][1] > cursor)
                    cursor = blocks[i][1];
            }

            if (limit - cursor >= height - Eps && Supported(placed, x, dx, z, dz, cursor))
            {
                y = cursor;
                return true;
            }

            y = 0m;
            return false;
        }


        static bool Supported(List<Item> placed, decimal x, decimal dx, decimal z, decimal dz, decimal y)
        {
            if (y <= Eps)
                return true;

            var free = new List<Rect> { new Rect(x, z, dx, dz) };
            for (int i = 0; i < placed.Count; i++)
            {
                Item p = placed[i];
                if (Abs((p.CoordY + p.PackDimY) - y) > Eps)
                    continue;

                var next = new List<Rect>();
                for (int r = 0; r < free.Count; r++)
                    free[r].Subtract(p.CoordX, p.CoordZ, p.PackDimX, p.PackDimZ, next);
                free = next;
                if (free.Count == 0)
                    return true;
            }

            return false;
        }


        static decimal Locked(Item item, bool longestOnZ)
        {
            if (longestOnZ)
                return Max(item.Dim1, Max(item.Dim2, item.Dim3));
            return Min(item.Dim1, Min(item.Dim2, item.Dim3));
        }


        static decimal FootprintArea(Item item, bool longestOnZ)
        {
            OtherSides(item, Locked(item, longestOnZ), out decimal a, out decimal b);
            return a * b;
        }


        static void OtherSides(Item item, decimal vertical, out decimal a, out decimal b)
        {
            var dims = new List<decimal> { item.Dim1, item.Dim2, item.Dim3 };
            dims.RemoveAt(dims.FindIndex(d => d == vertical));
            a = dims[0];
            b = dims[1];
        }


        static void AddUnique(List<decimal> values, decimal value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (Abs(values[i] - value) <= Eps)
                    return;
            }
            values.Add(value);
        }


        static bool RangesOverlap(decimal a, decimal aLen, decimal b, decimal bLen)
        {
            return a < b + bLen - Eps && b < a + aLen - Eps;
        }


        static decimal Abs(decimal value)
        {
            return value < 0m ? -value : value;
        }


        static decimal Max(decimal a, decimal b)
        {
            return a > b ? a : b;
        }


        static decimal Min(decimal a, decimal b)
        {
            return a < b ? a : b;
        }


        readonly struct Rect
        {
            public readonly decimal X;
            public readonly decimal Z;
            public readonly decimal Dx;
            public readonly decimal Dz;

            public Rect(decimal x, decimal z, decimal dx, decimal dz)
            {
                X = x;
                Z = z;
                Dx = dx;
                Dz = dz;
            }

            public void Subtract(decimal cx, decimal cz, decimal cdx, decimal cdz, List<Rect> dest)
            {
                decimal ix0 = X > cx ? X : cx;
                decimal iz0 = Z > cz ? Z : cz;
                decimal ix1 = (X + Dx) < (cx + cdx) ? (X + Dx) : (cx + cdx);
                decimal iz1 = (Z + Dz) < (cz + cdz) ? (Z + Dz) : (cz + cdz);
                if (ix1 - ix0 <= Eps || iz1 - iz0 <= Eps)
                {
                    dest.Add(this);
                    return;
                }

                if (ix0 - X > Eps)
                    dest.Add(new Rect(X, Z, ix0 - X, Dz));
                if (X + Dx - ix1 > Eps)
                    dest.Add(new Rect(ix1, Z, X + Dx - ix1, Dz));
                if (iz0 - Z > Eps)
                    dest.Add(new Rect(ix0, Z, ix1 - ix0, iz0 - Z));
                if (Z + Dz - iz1 > Eps)
                    dest.Add(new Rect(ix0, iz1, ix1 - ix0, Z + Dz - iz1));
            }
        }
    }
}
