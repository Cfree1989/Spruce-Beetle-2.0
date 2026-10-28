using System;
using System.Collections.Generic;
using CromulentBisgetti.ContainerPacking.Entities;

namespace SpruceBeetle.Packing
{
    /// <summary>
    /// Stands every scrap on its longest side with its thickness on one plan axis, so every wide face points the same way
    /// and boards form layers that touch face to face. A board's side edges stay at least the bond distance from the seams
    /// in the layers it touches (a running bond in plan), and its top misses side neighbors' tops by the stagger distance
    /// when it can. Each piece sits on the floor or on a complete platform.
    /// </summary>
    static class BondPacker
    {
        const double Eps = 1e-6;


        public static BondPack Pack(Container container, List<Item> items, double stagger, double bond)
        {
            if (stagger < 0)
                stagger = 0;
            if (bond < 0)
                bond = 0;

            BondPack throughX = PackThrough(container, items, stagger, bond, true);
            BondPack throughY = PackThrough(container, items, stagger, bond, false);

            if (throughY.Volume > throughX.Volume + Eps)
                return throughY;
            if (throughY.Volume >= throughX.Volume - Eps && throughY.Groups < throughX.Groups)
                return throughY;
            return throughX;
        }


        /// <summary>
        /// Groups of packed pieces joined through a shared wide face. Two pieces join when the face they touch on is
        /// perpendicular to both thicknesses and the contact is at least <paramref name="overlap"/> across and in height.
        /// </summary>
        internal static int CountGroups(List<Item> placed, double overlap)
        {
            int n = placed.Count;
            var parent = new int[n];
            for (int i = 0; i < n; i++)
                parent[i] = i;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    if (FaceJoined(placed[i], placed[j], overlap))
                        Union(parent, i, j);
                }
            }

            int groups = 0;
            for (int i = 0; i < n; i++)
            {
                if (Find(parent, i) == i)
                    groups++;
            }
            return groups;
        }


        internal static List<string> Check()
        {
            var fails = new List<string>();
            CheckSmallColumn(fails);
            CheckNoBond(fails);
            CheckFullColumn(fails);
            CheckFillers(fails);
            return fails;
        }


        static void CheckSmallColumn(List<string> fails)
        {
            var container = new Container(0, 7m, 7.5m, 48m);
            BondPack pack = Pack(container, Studs(40, 1.5m, 3.5m, new[] { 12m, 16m, 20m, 24m, 30m, 36m, 42m, 48m }), 4, 1.5);
            Expect(fails, "small", Valid(pack.Placed, container), "pieces stay inside, do not overlap, and sit on a full platform");
            Expect(fails, "small", pack.Groups == 1, $"one group through wide faces, got {pack.Groups}");
            Expect(fails, "small", pack.LinedUp == 0, $"no side edge within 1.5 of a seam in a touching layer, got {pack.LinedUp}");
            Expect(fails, "small", OneThicknessAxis(pack.Placed), "every thickness on the same plan axis");
        }


        static void CheckNoBond(List<string> fails)
        {
            var container = new Container(0, 7m, 7.5m, 48m);
            BondPack pack = Pack(container, Studs(40, 1.5m, 3.5m, new[] { 12m, 16m, 20m, 24m, 30m, 36m, 42m, 48m }), 4, 0);
            Expect(fails, "no bond", Valid(pack.Placed, container), "pieces stay inside, do not overlap, and sit on a full platform");
            Expect(fails, "no bond", pack.Groups > 1, $"seams line up and the pack splits, got {pack.Groups} group(s)");
        }


        static void CheckFullColumn(List<string> fails)
        {
            var lengths = new decimal[25];
            for (int i = 0; i < lengths.Length; i++)
                lengths[i] = 24m + 3m * i;
            var container = new Container(0, 24m, 24m, 96m);
            BondPack pack = Pack(container, Studs(260, 1.5m, 3.5m, lengths), 4, 1.5);
            Expect(fails, "column", Valid(pack.Placed, container), "pieces stay inside, do not overlap, and sit on a full platform");
            Expect(fails, "column", pack.Groups == 1, $"one group through wide faces, got {pack.Groups}");
            Expect(fails, "column", pack.LinedUp == 0, $"no side edge within 1.5 of a seam in a touching layer, got {pack.LinedUp}");
        }


        static void CheckFillers(List<string> fails)
        {
            var container = new Container(0, 7m, 7.5m, 24m);
            List<Item> items = Studs(10, 1.5m, 3.5m, new[] { 24m });
            List<Item> narrow = Studs(6, 1.5m, 1.5m, new[] { 24m });
            for (int i = 0; i < narrow.Count; i++)
                items.Add(new Item(items.Count, narrow[i].Dim1, narrow[i].Dim2, narrow[i].Dim3, 1));

            BondPack pack = Pack(container, items, 4, 1.5);
            int fillers = 0;
            for (int i = 0; i < pack.Placed.Count; i++)
            {
                if (pack.Placed[i].PackDimX == 1.5m && pack.Placed[i].PackDimZ == 1.5m)
                    fillers++;
            }

            Expect(fails, "fillers", Valid(pack.Placed, container), "pieces stay inside, do not overlap, and sit on a full platform");
            Expect(fails, "fillers", fillers >= 4, $"1.5 × 1.5 scraps fill the edge gaps beside offset boards, got {fillers}");
            Expect(fails, "fillers", pack.Groups == 1, $"one group through wide faces, got {pack.Groups}");
        }


        static List<Item> Studs(int count, decimal t, decimal w, decimal[] lengths)
        {
            var items = new List<Item>(count);
            for (int i = 0; i < count; i++)
                items.Add(new Item(i, t, w, lengths[i % lengths.Length], 1));
            return items;
        }


        static bool OneThicknessAxis(List<Item> placed)
        {
            bool alongX = false;
            bool alongZ = false;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].PackDimX < placed[i].PackDimZ)
                    alongX = true;
                else if (placed[i].PackDimZ < placed[i].PackDimX)
                    alongZ = true;
            }
            return !(alongX && alongZ);
        }


        static bool Valid(List<Item> placed, Container container)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                Item p = placed[i];
                if (p.CoordX < 0 || p.CoordY < 0 || p.CoordZ < 0)
                    return false;
                if (p.CoordX + p.PackDimX > container.Length || p.CoordY + p.PackDimY > container.Height || p.CoordZ + p.PackDimZ > container.Width)
                    return false;

                for (int j = i + 1; j < placed.Count; j++)
                {
                    Item q = placed[j];
                    if (Overlaps((double)p.CoordX, (double)p.PackDimX, (double)q.CoordX, (double)q.PackDimX)
                        && Overlaps((double)p.CoordY, (double)p.PackDimY, (double)q.CoordY, (double)q.PackDimY)
                        && Overlaps((double)p.CoordZ, (double)p.PackDimZ, (double)q.CoordZ, (double)q.PackDimZ))
                        return false;
                }

                if (p.CoordY == 0)
                    continue;

                var free = new List<Rect> { new Rect((double)p.CoordX, (double)p.CoordZ, (double)p.PackDimX, (double)p.PackDimZ) };
                for (int j = 0; j < placed.Count && free.Count > 0; j++)
                {
                    Item q = placed[j];
                    if (q.CoordY + q.PackDimY != p.CoordY)
                        continue;
                    var next = new List<Rect>();
                    for (int r = 0; r < free.Count; r++)
                        free[r].Subtract((double)q.CoordX, (double)q.CoordZ, (double)q.PackDimX, (double)q.PackDimZ, next);
                    free = next;
                }
                if (free.Count > 0)
                    return false;
            }
            return true;
        }


        static void Expect(List<string> fails, string name, bool ok, string message)
        {
            if (!ok)
                fails.Add(name + ": " + message);
        }


        static BondPack PackThrough(Container container, List<Item> items, double stagger, double bond, bool throughX)
        {
            double spanA = (double)(throughX ? container.Length : container.Width);
            double spanB = (double)(throughX ? container.Width : container.Length);
            double height = (double)container.Height;

            var waiting = new List<Piece>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                var dims = new[] { (double)items[i].Dim1, (double)items[i].Dim2, (double)items[i].Dim3 };
                Array.Sort(dims);
                waiting.Add(new Piece(items[i], dims[0], dims[1], dims[2]));
            }

            bool uniformLengths = SameLength(waiting);
            var placed = new List<Board>();
            while (waiting.Count > 0)
            {
                if (!Next(waiting, placed, spanA, spanB, height, stagger, bond, out int pick, out Board board))
                    break;
                placed.Add(board);
                waiting.RemoveAt(pick);
            }

            var packed = new List<Item>(placed.Count);
            double volume = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                packed.Add(ToItem(placed[i], throughX));
                volume += placed[i].T * placed[i].W * placed[i].H;
            }

            return new BondPack(
                packed,
                throughX,
                volume,
                CountGroups(packed, bond),
                CountLinedUp(placed, bond, spanB),
                CountAligned(placed, stagger),
                uniformLengths);
        }


        /// <summary>
        /// Lowest flat level first, then the lowest layer. In that layer: a bonded spot, then the lowest spot along the layer,
        /// then a top that clears the neighbors, then the widest and longest scrap.
        /// </summary>
        static bool Next(
            List<Piece> waiting,
            List<Board> placed,
            double spanA,
            double spanB,
            double height,
            double stagger,
            double bond,
            out int pick,
            out Board board)
        {
            pick = -1;
            board = null;

            var spots = new List<Spot>();
            bool any = false;
            double yMin = 0;
            double aMin = 0;

            List<Section> sections = Sections(waiting);
            for (int s = 0; s < sections.Count; s++)
            {
                Section section = sections[s];
                List<double> starts = LayerStarts(placed, section.T, spanA);
                for (int ia = 0; ia < starts.Count; ia++)
                {
                    double a = starts[ia];
                    List<Board> layer = InLayer(placed, a, section.T);
                    List<double> offsets = Offsets(placed, layer, a, section.T, section.W, spanB, bond);
                    for (int ib = 0; ib < offsets.Count; ib++)
                    {
                        double b = offsets[ib];
                        if (!Level(layer, a, section.T, b, section.W, out double y))
                            continue;
                        if (y + section.MinH > height + Eps)
                            continue;

                        if (any)
                        {
                            if (y > yMin + Eps || (Math.Abs(y - yMin) <= Eps && a > aMin + Eps))
                                continue;
                            if (y < yMin - Eps || a < aMin - Eps)
                                spots.Clear();
                        }

                        any = true;
                        yMin = y;
                        aMin = a;
                        spots.Add(new Spot(section, a, b, y));
                    }
                }
            }

            if (!any)
                return false;

            Key best = default;
            for (int s = 0; s < spots.Count; s++)
            {
                Spot spot = spots[s];
                for (int i = 0; i < waiting.Count; i++)
                {
                    Piece piece = waiting[i];
                    if (!spot.Section.Holds(piece) || spot.Y + piece.H > height + Eps)
                        continue;

                    var candidate = new Board(piece.Item.ID, spot.A, spot.B, spot.Y, piece.T, piece.W, piece.H);
                    bool bonded = Bonded(placed, candidate, bond, spanB);
                    double gap = TopGap(placed, candidate, out bool hasNeighbor);
                    var key = new Key(bonded, spot.B, !hasNeighbor || gap + Eps >= stagger, gap, piece.W, piece.H, piece.Item.ID);
                    if (board != null && !key.Beats(best))
                        continue;

                    best = key;
                    board = candidate;
                    pick = i;
                }
            }

            return board != null;
        }


        static List<Section> Sections(List<Piece> waiting)
        {
            var sections = new List<Section>();
            for (int i = 0; i < waiting.Count; i++)
            {
                bool found = false;
                for (int s = 0; s < sections.Count; s++)
                {
                    if (!sections[s].Holds(waiting[i]))
                        continue;
                    if (waiting[i].H < sections[s].MinH)
                        sections[s] = new Section(sections[s].T, sections[s].W, waiting[i].H);
                    found = true;
                    break;
                }

                if (!found)
                    sections.Add(new Section(waiting[i].T, waiting[i].W, waiting[i].H));
            }

            return sections;
        }


        static List<double> LayerStarts(List<Board> placed, double t, double spanA)
        {
            var starts = new List<double>();
            AddIn(starts, 0, t, spanA);
            for (int i = 0; i < placed.Count; i++)
            {
                AddIn(starts, placed[i].A, t, spanA);
                AddIn(starts, placed[i].A1, t, spanA);
            }
            starts.Sort();
            return starts;
        }


        static List<Board> InLayer(List<Board> placed, double a, double t)
        {
            var layer = new List<Board>();
            for (int i = 0; i < placed.Count; i++)
            {
                if (Overlaps(a, t, placed[i].A, placed[i].T))
                    layer.Add(placed[i]);
            }
            return layer;
        }


        /// <summary>
        /// Spots along a layer: the walls, against or on top of boards in the layer, and centered on or offset by the bond
        /// from each seam in a layer this one touches face to face.
        /// </summary>
        static List<double> Offsets(List<Board> placed, List<Board> layer, double a, double t, double w, double spanB, double bond)
        {
            var offsets = new List<double>();
            AddIn(offsets, 0, w, spanB);
            AddIn(offsets, spanB - w, w, spanB);

            for (int i = 0; i < layer.Count; i++)
            {
                AddIn(offsets, layer[i].B, w, spanB);
                AddIn(offsets, layer[i].B1, w, spanB);
                AddIn(offsets, layer[i].B - w, w, spanB);
            }

            for (int i = 0; i < placed.Count; i++)
            {
                Board p = placed[i];
                if (!FaceTouch(a, t, p))
                    continue;

                var seams = new[] { p.B, p.B1 };
                for (int k = 0; k < seams.Length; k++)
                {
                    double s = seams[k];
                    if (AtWall(s, spanB))
                        continue;
                    AddIn(offsets, s - w / 2, w, spanB);
                    AddIn(offsets, s + bond, w, spanB);
                    AddIn(offsets, s - bond - w, w, spanB);
                }
            }

            return offsets;
        }


        /// <summary>
        /// Height of the tops under the footprint. Only a flat level counts, so the piece does not bridge a hole.
        /// Every packed piece is fully supported, so each plan point is filled from the floor to its top.
        /// </summary>
        static bool Level(List<Board> layer, double a, double t, double b, double w, out double y)
        {
            y = 0;
            for (int i = 0; i < layer.Count; i++)
            {
                if (Overlaps(b, w, layer[i].B, layer[i].W) && layer[i].Top > y)
                    y = layer[i].Top;
            }

            if (y <= Eps)
            {
                y = 0;
                return true;
            }

            var free = new List<Rect> { new Rect(a, b, t, w) };
            for (int i = 0; i < layer.Count; i++)
            {
                Board p = layer[i];
                if (Math.Abs(p.Top - y) > Eps)
                    continue;

                var next = new List<Rect>();
                for (int r = 0; r < free.Count; r++)
                    free[r].Subtract(p.A, p.B, p.T, p.W, next);
                free = next;
                if (free.Count == 0)
                    return true;
            }

            return false;
        }


        static bool Bonded(List<Board> placed, Board c, double bond, double spanB)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                Board p = placed[i];
                if (!FaceTouch(c.A, c.T, p) || !Overlaps(c.Y, c.H, p.Y, p.H))
                    continue;
                if (NearSeam(c, p, bond, spanB))
                    return false;
            }
            return true;
        }


        static bool NearSeam(Board c, Board p, double bond, double spanB)
        {
            var edges = new[] { c.B, c.B1 };
            var seams = new[] { p.B, p.B1 };
            for (int e = 0; e < edges.Length; e++)
            {
                if (AtWall(edges[e], spanB))
                    continue;
                for (int s = 0; s < seams.Length; s++)
                {
                    if (AtWall(seams[s], spanB))
                        continue;
                    if (Math.Abs(edges[e] - seams[s]) < bond - Eps)
                        return true;
                }
            }
            return false;
        }


        static double TopGap(List<Board> placed, Board c, out bool hasNeighbor)
        {
            hasNeighbor = false;
            double min = double.MaxValue;
            for (int i = 0; i < placed.Count; i++)
            {
                Board p = placed[i];
                if (!SideTouch(c, p) || !Overlaps(c.Y, c.H, p.Y, p.H))
                    continue;

                hasNeighbor = true;
                double gap = Math.Abs(c.Top - p.Top);
                if (gap < min)
                    min = gap;
            }
            return min;
        }


        static int CountLinedUp(List<Board> placed, double bond, double spanB)
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                for (int j = 0; j < placed.Count; j++)
                {
                    if (i == j)
                        continue;
                    Board c = placed[i];
                    Board p = placed[j];
                    if (!FaceTouch(c.A, c.T, p) || !Overlaps(c.Y, c.H, p.Y, p.H))
                        continue;
                    if (!NearSeam(c, p, bond, spanB))
                        continue;
                    count++;
                    break;
                }
            }
            return count;
        }


        static int CountAligned(List<Board> placed, double stagger)
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                for (int j = i + 1; j < placed.Count; j++)
                {
                    Board a = placed[i];
                    Board b = placed[j];
                    if (!SideTouch(a, b) || !Overlaps(a.Y, a.H, b.Y, b.H))
                        continue;
                    if (Math.Abs(a.Top - b.Top) + Eps < stagger)
                        count++;
                }
            }
            return count;
        }


        static bool FaceTouch(double a, double t, Board p)
        {
            return Math.Abs(p.A1 - a) <= Eps || Math.Abs(p.A - (a + t)) <= Eps;
        }


        static bool SideTouch(Board c, Board p)
        {
            bool touchA = (Math.Abs(p.A1 - c.A) <= Eps || Math.Abs(p.A - c.A1) <= Eps) && Overlaps(c.B, c.W, p.B, p.W);
            bool touchB = (Math.Abs(p.B1 - c.B) <= Eps || Math.Abs(p.B - c.B1) <= Eps) && Overlaps(c.A, c.T, p.A, p.T);
            return touchA || touchB;
        }


        static bool FaceJoined(Item a, Item b, double overlap)
        {
            if (!Enough(Overlap((double)a.CoordY, (double)a.PackDimY, (double)b.CoordY, (double)b.PackDimY), overlap))
                return false;

            double ax = (double)a.CoordX, adx = (double)a.PackDimX, az = (double)a.CoordZ, adz = (double)a.PackDimZ;
            double bx = (double)b.CoordX, bdx = (double)b.PackDimX, bz = (double)b.CoordZ, bdz = (double)b.PackDimZ;

            bool touchX = Math.Abs(ax + adx - bx) <= Eps || Math.Abs(bx + bdx - ax) <= Eps;
            if (touchX && adx <= adz + Eps && bdx <= bdz + Eps && Enough(Overlap(az, adz, bz, bdz), overlap))
                return true;

            bool touchZ = Math.Abs(az + adz - bz) <= Eps || Math.Abs(bz + bdz - az) <= Eps;
            return touchZ && adz <= adx + Eps && bdz <= bdx + Eps && Enough(Overlap(ax, adx, bx, bdx), overlap);
        }


        static bool Enough(double length, double need)
        {
            return length > Eps && length + Eps >= need;
        }


        static Item ToItem(Board board, bool throughX)
        {
            var item = new Item(board.Id, (decimal)board.T, (decimal)board.W, (decimal)board.H, 1);
            item.CoordY = Dec(board.Y);
            item.PackDimY = Dec(board.H);
            if (throughX)
            {
                item.CoordX = Dec(board.A);
                item.CoordZ = Dec(board.B);
                item.PackDimX = Dec(board.T);
                item.PackDimZ = Dec(board.W);
            }
            else
            {
                item.CoordX = Dec(board.B);
                item.CoordZ = Dec(board.A);
                item.PackDimX = Dec(board.W);
                item.PackDimZ = Dec(board.T);
            }
            item.IsPacked = true;
            return item;
        }


        static decimal Dec(double value)
        {
            return (decimal)Math.Round(value, 6);
        }


        static bool SameLength(List<Piece> pieces)
        {
            if (pieces.Count < 2)
                return false;
            for (int i = 1; i < pieces.Count; i++)
            {
                if (Math.Abs(pieces[i].H - pieces[0].H) > Eps)
                    return false;
            }
            return true;
        }


        static void AddIn(List<double> values, double value, double size, double span)
        {
            if (value < -Eps || value + size > span + Eps)
                return;
            if (value < 0)
                value = 0;
            for (int i = 0; i < values.Count; i++)
            {
                if (Math.Abs(values[i] - value) <= Eps)
                    return;
            }
            values.Add(value);
        }


        static bool AtWall(double value, double span)
        {
            return value <= Eps || value >= span - Eps;
        }


        static bool Overlaps(double a, double aLen, double b, double bLen)
        {
            return a < b + bLen - Eps && b < a + aLen - Eps;
        }


        static double Overlap(double a, double aLen, double b, double bLen)
        {
            double length = Math.Min(a + aLen, b + bLen) - Math.Max(a, b);
            return length > 0 ? length : 0;
        }


        static int Find(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }


        static void Union(int[] parent, int i, int j)
        {
            int ri = Find(parent, i);
            int rj = Find(parent, j);
            if (ri != rj)
                parent[rj] = ri;
        }


        /// <summary>
        /// A packed board in plan axes: A is through the thickness (the dowel direction), B runs along the layer.
        /// </summary>
        sealed class Board
        {
            public Board(int id, double a, double b, double y, double t, double w, double h)
            {
                Id = id;
                A = a;
                B = b;
                Y = y;
                T = t;
                W = w;
                H = h;
            }

            public int Id { get; }
            public double A { get; }
            public double B { get; }
            public double Y { get; }
            public double T { get; }
            public double W { get; }
            public double H { get; }
            public double A1 => A + T;
            public double B1 => B + W;
            public double Top => Y + H;
        }


        sealed class Piece
        {
            public Piece(Item item, double t, double w, double h)
            {
                Item = item;
                T = t;
                W = w;
                H = h;
            }

            public Item Item { get; }
            public double T { get; }
            public double W { get; }
            public double H { get; }
        }


        readonly struct Section
        {
            public Section(double t, double w, double minH)
            {
                T = t;
                W = w;
                MinH = minH;
            }

            public double T { get; }
            public double W { get; }
            public double MinH { get; }

            public bool Holds(Piece piece)
            {
                return Math.Abs(piece.T - T) <= Eps && Math.Abs(piece.W - W) <= Eps;
            }
        }


        readonly struct Spot
        {
            public Spot(Section section, double a, double b, double y)
            {
                Section = section;
                A = a;
                B = b;
                Y = y;
            }

            public Section Section { get; }
            public double A { get; }
            public double B { get; }
            public double Y { get; }
        }


        readonly struct Key
        {
            public Key(bool bonded, double b, bool clears, double gap, double width, double length, int id)
            {
                Bonded = bonded;
                B = b;
                Clears = clears;
                Gap = gap;
                Width = width;
                Length = length;
                Id = id;
            }

            public bool Bonded { get; }
            public double B { get; }
            public bool Clears { get; }
            public double Gap { get; }
            public double Width { get; }
            public double Length { get; }
            public int Id { get; }

            public bool Beats(Key other)
            {
                if (Bonded != other.Bonded)
                    return Bonded;
                if (Math.Abs(B - other.B) > Eps)
                    return B < other.B;
                if (Clears != other.Clears)
                    return Clears;
                if (!Clears && Math.Abs(Gap - other.Gap) > Eps)
                    return Gap > other.Gap;
                if (Math.Abs(Width - other.Width) > Eps)
                    return Width > other.Width;
                if (Math.Abs(Length - other.Length) > Eps)
                    return Length > other.Length;
                return Id < other.Id;
            }
        }


        readonly struct Rect
        {
            readonly double x, z, dx, dz;

            public Rect(double x, double z, double dx, double dz)
            {
                this.x = x;
                this.z = z;
                this.dx = dx;
                this.dz = dz;
            }

            public void Subtract(double cx, double cz, double cdx, double cdz, List<Rect> dest)
            {
                double ix0 = Math.Max(x, cx);
                double iz0 = Math.Max(z, cz);
                double ix1 = Math.Min(x + dx, cx + cdx);
                double iz1 = Math.Min(z + dz, cz + cdz);
                if (ix1 - ix0 <= Eps || iz1 - iz0 <= Eps)
                {
                    dest.Add(this);
                    return;
                }

                if (ix0 - x > Eps)
                    dest.Add(new Rect(x, z, ix0 - x, dz));
                if (x + dx - ix1 > Eps)
                    dest.Add(new Rect(ix1, z, x + dx - ix1, dz));
                if (iz0 - z > Eps)
                    dest.Add(new Rect(ix0, z, ix1 - ix0, iz0 - z));
                if (z + dz - iz1 > Eps)
                    dest.Add(new Rect(ix0, iz1, ix1 - ix0, z + dz - iz1));
            }
        }
    }


    internal readonly struct BondPack
    {
        public BondPack(List<Item> placed, bool throughX, double volume, int groups, int linedUp, int alignedSideJoints, bool uniformLengths)
        {
            Placed = placed;
            ThroughX = throughX;
            Volume = volume;
            Groups = groups;
            LinedUp = linedUp;
            AlignedSideJoints = alignedSideJoints;
            UniformLengths = uniformLengths;
        }

        public List<Item> Placed { get; }
        public bool ThroughX { get; }
        public double Volume { get; }
        public int Groups { get; }
        public int LinedUp { get; }
        public int AlignedSideJoints { get; }
        public bool UniformLengths { get; }
    }
}
