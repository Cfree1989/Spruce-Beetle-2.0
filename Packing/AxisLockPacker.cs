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

        /// <summary>
        /// A spine must reach this share of the column height. Shorter scraps are used only when the pile has fewer long ones.
        /// </summary>
        internal const decimal SpineFraction = 0.75m;


        public static List<Item> Pack(Container container, List<Item> items, bool longestOnZ)
        {
            List<Item> waiting = CopyItems(items);
            waiting.Sort((a, b) => CompareLocked(a, b, longestOnZ));

            var placed = new List<Item>();
            PackWaiting(container, waiting, placed, longestOnZ);
            return placed;
        }


        /// <summary>
        /// Longest side on Z. At the lowest solid height, take the longest scrap whose top misses every side neighbor by at least <paramref name="stagger"/>.
        /// </summary>
        public static StaggerPack PackStagger(Container container, List<Item> items, decimal stagger)
        {
            if (stagger < 0m)
                stagger = 0m;

            List<Item> waiting = CopyItems(items);
            bool uniformLengths = SameLockedLength(waiting);
            var placed = new List<Item>();

            bool progress = true;
            while (progress)
            {
                progress = false;
                var poses = new Pose[waiting.Count];
                decimal yMin = 0m;
                bool any = false;

                for (int i = 0; i < waiting.Count; i++)
                {
                    poses[i] = FindPose(container, placed, waiting[i], true);
                    if (!poses[i].Found)
                        continue;
                    if (!any || poses[i].Y < yMin - Eps)
                        yMin = poses[i].Y;
                    any = true;
                }

                if (!any)
                    break;

                int best = -1;
                bool bestClears = false;
                decimal bestHeight = 0m;
                decimal bestGap = 0m;
                for (int i = 0; i < waiting.Count; i++)
                {
                    if (!poses[i].Found || poses[i].Y > yMin + Eps)
                        continue;

                    decimal gap = MinTopGap(placed, poses[i], out bool hasNeighbor);
                    bool clears = !hasNeighbor || gap + Eps >= stagger;
                    if (best >= 0 && !PreferStagger(clears, poses[i].Height, gap, waiting[i].ID, bestClears, bestHeight, bestGap, waiting[best].ID))
                        continue;

                    best = i;
                    bestClears = clears;
                    bestHeight = poses[i].Height;
                    bestGap = gap;
                }

                if (best < 0)
                    break;

                Apply(waiting[best], poses[best]);
                placed.Add(waiting[best]);
                waiting.RemoveAt(best);
                progress = true;
            }

            return new StaggerPack(placed, CountAligned(placed, stagger), uniformLengths);
        }


        /// <summary>
        /// Stands up to four long scraps on the floor, spread to the corners, then fills the rest longest-first.
        /// </summary>
        public static SpinePack PackSpines(Container container, List<Item> items, int spineCount)
        {
            if (spineCount < 1)
                spineCount = 1;
            if (spineCount > 4)
                spineCount = 4;

            List<Item> waiting = CopyItems(items);
            waiting.Sort((a, b) => CompareLocked(a, b, true));

            decimal minimum = container.Height * SpineFraction;
            var placed = new List<Item>();
            var spineLengths = new List<decimal>();
            var shortLengths = new List<decimal>();

            int index = 0;
            while (placed.Count < spineCount && index < waiting.Count)
            {
                Item item = waiting[index];
                decimal height = Locked(item, true);
                if (height > container.Height + Eps || !TryPlaceSpine(container, placed, item))
                {
                    index++;
                    continue;
                }

                spineLengths.Add(height);
                if (height + Eps < minimum)
                    shortLengths.Add(height);
                placed.Add(item);
                waiting.RemoveAt(index);
            }

            PackWaiting(container, waiting, placed, true);
            return new SpinePack(placed, spineLengths, shortLengths, minimum, spineCount);
        }


        static void PackWaiting(Container container, List<Item> waiting, List<Item> placed, bool longestOnZ)
        {
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
        }


        static bool TryPlace(Container container, List<Item> placed, Item item, bool longestOnZ)
        {
            Pose pose = FindPose(container, placed, item, longestOnZ);
            if (!pose.Found)
                return false;

            Apply(item, pose);
            return true;
        }


        static Pose FindPose(Container container, List<Item> placed, Item item, bool longestOnZ)
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
                return Pose.None;

            return new Pose(true, bestY, bestX, bestZ, bestDx, bestDz, height);
        }


        static void Apply(Item item, Pose pose)
        {
            item.CoordX = pose.X;
            item.CoordY = pose.Y;
            item.CoordZ = pose.Z;
            item.PackDimX = pose.Dx;
            item.PackDimY = pose.Height;
            item.PackDimZ = pose.Dz;
            item.IsPacked = true;
        }


        static bool TryPlaceSpine(Container container, List<Item> placed, Item item)
        {
            decimal height = Locked(item, true);
            if (height > container.Height + Eps)
                return false;

            OtherSides(item, height, out decimal u, out decimal v);
            int start = placed.Count;
            if (start > 3)
                start = 3;

            for (int k = 0; k < 4; k++)
            {
                int corner = (start + k) % 4;
                if (TryCorner(container, placed, corner, height, u, v, out Pose pose)
                    || (u != v && TryCorner(container, placed, corner, height, v, u, out pose)))
                {
                    Apply(item, pose);
                    return true;
                }
            }

            if (TryFarthestFloor(container, placed, height, u, v, out Pose far)
                || (u != v && TryFarthestFloor(container, placed, height, v, u, out far)))
            {
                Apply(item, far);
                return true;
            }

            return false;
        }


        static bool TryCorner(Container container, List<Item> placed, int corner, decimal height, decimal dx, decimal dz, out Pose pose)
        {
            decimal x;
            decimal z;
            switch (corner)
            {
                case 0:
                    x = 0m;
                    z = 0m;
                    break;
                case 1:
                    x = container.Length - dx;
                    z = container.Width - dz;
                    break;
                case 2:
                    x = container.Length - dx;
                    z = 0m;
                    break;
                default:
                    x = 0m;
                    z = container.Width - dz;
                    break;
            }

            if (!FitsFloor(container, placed, x, dx, z, dz, height))
            {
                pose = Pose.None;
                return false;
            }

            pose = new Pose(true, 0m, x, z, dx, dz, height);
            return true;
        }


        static bool TryFarthestFloor(Container container, List<Item> placed, decimal height, decimal dx, decimal dz, out Pose pose)
        {
            var xs = new List<decimal> { 0m };
            var zs = new List<decimal> { 0m };
            for (int i = 0; i < placed.Count; i++)
            {
                AddUnique(xs, placed[i].CoordX + placed[i].PackDimX);
                AddUnique(zs, placed[i].CoordZ + placed[i].PackDimZ);
            }

            bool found = false;
            decimal bestX = 0m;
            decimal bestZ = 0m;
            decimal bestScore = 0m;
            for (int ix = 0; ix < xs.Count; ix++)
            {
                decimal x = xs[ix];
                for (int iz = 0; iz < zs.Count; iz++)
                {
                    decimal z = zs[iz];
                    if (!FitsFloor(container, placed, x, dx, z, dz, height))
                        continue;

                    decimal score = Separation(placed, x, dx, z, dz);
                    if (!found || score > bestScore + Eps || (Abs(score - bestScore) <= Eps && (x < bestX - Eps || (Abs(x - bestX) <= Eps && z < bestZ))))
                    {
                        found = true;
                        bestX = x;
                        bestZ = z;
                        bestScore = score;
                    }
                }
            }

            if (!found)
            {
                pose = Pose.None;
                return false;
            }

            pose = new Pose(true, 0m, bestX, bestZ, dx, dz, height);
            return true;
        }


        static bool FitsFloor(Container container, List<Item> placed, decimal x, decimal dx, decimal z, decimal dz, decimal height)
        {
            if (x < -Eps || z < -Eps)
                return false;
            if (x + dx > container.Length + Eps || z + dz > container.Width + Eps)
                return false;
            if (height > container.Height + Eps)
                return false;

            for (int i = 0; i < placed.Count; i++)
            {
                Item other = placed[i];
                if (RangesOverlap(x, dx, other.CoordX, other.PackDimX) && RangesOverlap(z, dz, other.CoordZ, other.PackDimZ))
                    return false;
            }

            return true;
        }


        static decimal Separation(List<Item> placed, decimal x, decimal dx, decimal z, decimal dz)
        {
            if (placed.Count == 0)
                return 0m;

            decimal cx = x + dx / 2m;
            decimal cz = z + dz / 2m;
            decimal min = decimal.MaxValue;
            for (int i = 0; i < placed.Count; i++)
            {
                Item other = placed[i];
                decimal px = other.CoordX + other.PackDimX / 2m;
                decimal pz = other.CoordZ + other.PackDimZ / 2m;
                decimal ddx = cx - px;
                decimal ddz = cz - pz;
                decimal distance = ddx * ddx + ddz * ddz;
                if (distance < min)
                    min = distance;
            }

            return min;
        }


        static bool PreferStagger(
            bool clears,
            decimal height,
            decimal gap,
            int id,
            bool bestClears,
            decimal bestHeight,
            decimal bestGap,
            int bestId)
        {
            if (clears != bestClears)
                return clears;

            if (clears)
            {
                if (Abs(height - bestHeight) > Eps)
                    return height > bestHeight;
                return id < bestId;
            }

            if (Abs(gap - bestGap) > Eps)
                return gap > bestGap;
            if (Abs(height - bestHeight) > Eps)
                return height > bestHeight;
            return id < bestId;
        }


        static decimal MinTopGap(List<Item> placed, Pose pose, out bool hasNeighbor)
        {
            hasNeighbor = false;
            decimal min = decimal.MaxValue;
            decimal top = pose.Y + pose.Height;
            for (int i = 0; i < placed.Count; i++)
            {
                Item other = placed[i];
                if (!SideTouch(pose.X, pose.Dx, pose.Z, pose.Dz, other.CoordX, other.PackDimX, other.CoordZ, other.PackDimZ))
                    continue;
                if (!RangesOverlap(pose.Y, pose.Height, other.CoordY, other.PackDimY))
                    continue;

                hasNeighbor = true;
                decimal gap = Abs(top - (other.CoordY + other.PackDimY));
                if (gap < min)
                    min = gap;
            }

            return min;
        }


        static int CountAligned(List<Item> placed, decimal stagger)
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                Item a = placed[i];
                for (int j = i + 1; j < placed.Count; j++)
                {
                    Item b = placed[j];
                    if (!SideTouch(a.CoordX, a.PackDimX, a.CoordZ, a.PackDimZ, b.CoordX, b.PackDimX, b.CoordZ, b.PackDimZ))
                        continue;
                    if (!RangesOverlap(a.CoordY, a.PackDimY, b.CoordY, b.PackDimY))
                        continue;
                    decimal gap = Abs((a.CoordY + a.PackDimY) - (b.CoordY + b.PackDimY));
                    if (gap + Eps < stagger)
                        count++;
                }
            }

            return count;
        }


        static bool SideTouch(
            decimal ax, decimal adx, decimal az, decimal adz,
            decimal bx, decimal bdx, decimal bz, decimal bdz)
        {
            decimal overlapX = OverlapLength(ax, adx, bx, bdx);
            decimal overlapZ = OverlapLength(az, adz, bz, bdz);
            decimal gapX = RangeGap(ax, adx, bx, bdx);
            decimal gapZ = RangeGap(az, adz, bz, bdz);
            bool touchX = gapX <= Eps && overlapZ > Eps && overlapX <= Eps;
            bool touchZ = gapZ <= Eps && overlapX > Eps && overlapZ <= Eps;
            return touchX || touchZ;
        }


        static decimal OverlapLength(decimal a, decimal aLen, decimal b, decimal bLen)
        {
            decimal start = Max(a, b);
            decimal end = Min(a + aLen, b + bLen);
            decimal length = end - start;
            return length > 0m ? length : 0m;
        }


        static decimal RangeGap(decimal a, decimal aLen, decimal b, decimal bLen)
        {
            if (a + aLen < b)
                return b - (a + aLen);
            if (b + bLen < a)
                return a - (b + bLen);
            return 0m;
        }


        static List<Item> CopyItems(List<Item> items)
        {
            var waiting = new List<Item>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                Item source = items[i];
                waiting.Add(new Item(source.ID, source.Dim1, source.Dim2, source.Dim3, 1));
            }

            return waiting;
        }


        static int CompareLocked(Item a, Item b, bool longestOnZ)
        {
            int cmp = Locked(b, longestOnZ).CompareTo(Locked(a, longestOnZ));
            if (cmp != 0)
                return cmp;
            cmp = FootprintArea(b, longestOnZ).CompareTo(FootprintArea(a, longestOnZ));
            if (cmp != 0)
                return cmp;
            return a.ID.CompareTo(b.ID);
        }


        static bool SameLockedLength(List<Item> items)
        {
            if (items.Count < 2)
                return false;

            decimal first = Locked(items[0], true);
            for (int i = 1; i < items.Count; i++)
            {
                if (Abs(Locked(items[i], true) - first) > Eps)
                    return false;
            }

            return true;
        }


        readonly struct Pose
        {
            public readonly bool Found;
            public readonly decimal Y;
            public readonly decimal X;
            public readonly decimal Z;
            public readonly decimal Dx;
            public readonly decimal Dz;
            public readonly decimal Height;

            public Pose(bool found, decimal y, decimal x, decimal z, decimal dx, decimal dz, decimal height)
            {
                Found = found;
                Y = y;
                X = x;
                Z = z;
                Dx = dx;
                Dz = dz;
                Height = height;
            }

            public static Pose None => new Pose(false, 0m, 0m, 0m, 0m, 0m, 0m);
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


    internal readonly struct StaggerPack
    {
        public StaggerPack(List<Item> placed, int alignedSideJoints, bool uniformLengths)
        {
            Placed = placed;
            AlignedSideJoints = alignedSideJoints;
            UniformLengths = uniformLengths;
        }

        public List<Item> Placed { get; }
        public int AlignedSideJoints { get; }
        public bool UniformLengths { get; }
    }


    internal readonly struct SpinePack
    {
        public SpinePack(List<Item> placed, List<decimal> spineLengths, List<decimal> shortSpineLengths, decimal minimumSpineLength, int requested)
        {
            Placed = placed;
            SpineLengths = spineLengths;
            ShortSpineLengths = shortSpineLengths;
            MinimumSpineLength = minimumSpineLength;
            Requested = requested;
        }

        public List<Item> Placed { get; }
        public List<decimal> SpineLengths { get; }
        public List<decimal> ShortSpineLengths { get; }
        public decimal MinimumSpineLength { get; }
        public int Requested { get; }
    }
}
