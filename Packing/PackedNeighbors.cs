using System;
using System.Collections.Generic;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public enum ContactAxis
    {
        X,
        Y,
        Z
    }


    public class PackedContact
    {
        public int IndexA { get; set; }
        public int IndexB { get; set; }
        public ContactAxis Axis { get; set; }
        public Plane Plane { get; set; }
        public BoundingBox Overlap { get; set; }
        public double Area { get; set; }

        public PackedContact Duplicate()
        {
            return new PackedContact
            {
                IndexA = IndexA,
                IndexB = IndexB,
                Axis = Axis,
                Plane = Plane,
                Overlap = Overlap,
                Area = Area
            };
        }

        public string AxisName => Axis.ToString();

        public Curve OverlapRectangle()
        {
            BoundingBox box = Overlap;
            if (!box.IsValid)
                return null;

            Point3d[] corners;
            switch (Axis)
            {
                case ContactAxis.X:
                    corners = new[]
                    {
                        new Point3d(box.Min.X, box.Min.Y, box.Min.Z),
                        new Point3d(box.Min.X, box.Max.Y, box.Min.Z),
                        new Point3d(box.Min.X, box.Max.Y, box.Max.Z),
                        new Point3d(box.Min.X, box.Min.Y, box.Max.Z),
                        new Point3d(box.Min.X, box.Min.Y, box.Min.Z)
                    };
                    break;
                case ContactAxis.Y:
                    corners = new[]
                    {
                        new Point3d(box.Min.X, box.Min.Y, box.Min.Z),
                        new Point3d(box.Max.X, box.Min.Y, box.Min.Z),
                        new Point3d(box.Max.X, box.Min.Y, box.Max.Z),
                        new Point3d(box.Min.X, box.Min.Y, box.Max.Z),
                        new Point3d(box.Min.X, box.Min.Y, box.Min.Z)
                    };
                    break;
                default:
                    corners = new[]
                    {
                        new Point3d(box.Min.X, box.Min.Y, box.Min.Z),
                        new Point3d(box.Max.X, box.Min.Y, box.Min.Z),
                        new Point3d(box.Max.X, box.Max.Y, box.Min.Z),
                        new Point3d(box.Min.X, box.Max.Y, box.Min.Z),
                        new Point3d(box.Min.X, box.Min.Y, box.Min.Z)
                    };
                    break;
            }

            return new Polyline(corners).ToNurbsCurve();
        }
    }


    internal static class PackedNeighbors
    {
        public static BoundingBox WorldBox(Offcut offcut)
        {
            if (offcut?.OffcutGeometry == null)
                return BoundingBox.Empty;

            return offcut.OffcutGeometry.GetBoundingBox(true);
        }


        public static bool ZContact(BoundingBox a, BoundingBox b, double tolerance, out Plane contact)
        {
            contact = Plane.Unset;

            if (!TryZContact(a, b, tolerance, out BoundingBox overlap, out Plane plane))
                return false;

            contact = plane;
            _ = overlap;
            return true;
        }


        public static List<PackedContact> AllContacts(List<Offcut> packed, double tolerance)
        {
            var contacts = new List<PackedContact>();
            int count = packed.Count;
            var boxes = new BoundingBox[count];
            for (int i = 0; i < count; i++)
                boxes[i] = WorldBox(packed[i]);

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (TryZContact(boxes[i], boxes[j], tolerance, out BoundingBox zOverlap, out Plane zPlane))
                    {
                        contacts.Add(MakeContact(i, j, ContactAxis.Z, zPlane, zOverlap));
                        continue;
                    }

                    if (TryXContact(boxes[i], boxes[j], tolerance, out BoundingBox xOverlap, out Plane xPlane))
                    {
                        contacts.Add(MakeContact(i, j, ContactAxis.X, xPlane, xOverlap));
                        continue;
                    }

                    if (TryYContact(boxes[i], boxes[j], tolerance, out BoundingBox yOverlap, out Plane yPlane))
                        contacts.Add(MakeContact(i, j, ContactAxis.Y, yPlane, yOverlap));
                }
            }

            return contacts;
        }


        /// <summary>
        /// Overlap boxes of neighbors sitting on the face of <paramref name="boxes"/>[<paramref name="index"/>]
        /// whose outward normal is <paramref name="outward"/>. Checks X, Y, and Z independently
        /// (unlike <see cref="AllContacts"/>, which reports only one axis per pair).
        /// </summary>
        public static List<BoundingBox> NeighborOverlapsOnFace(BoundingBox[] boxes, int index, Vector3d outward, double tolerance)
        {
            var overlaps = new List<BoundingBox>();
            if (boxes == null || index < 0 || index >= boxes.Length)
                return overlaps;

            BoundingBox self = boxes[index];
            if (!self.IsValid)
                return overlaps;

            Vector3d n = outward;
            if (!n.Unitize())
                return overlaps;

            double ax = Math.Abs(n.X);
            double ay = Math.Abs(n.Y);
            double az = Math.Abs(n.Z);

            for (int j = 0; j < boxes.Length; j++)
            {
                if (j == index)
                    continue;

                BoundingBox other = boxes[j];
                if (!other.IsValid)
                    continue;

                BoundingBox overlap;
                bool onThisSide;
                if (ax >= ay && ax >= az)
                {
                    if (!TryXContact(self, other, tolerance, out overlap, out _))
                        continue;
                    onThisSide = n.X > 0
                        ? Near(overlap.Min.X, self.Max.X, tolerance)
                        : Near(overlap.Min.X, self.Min.X, tolerance);
                }
                else if (ay >= az)
                {
                    if (!TryYContact(self, other, tolerance, out overlap, out _))
                        continue;
                    onThisSide = n.Y > 0
                        ? Near(overlap.Min.Y, self.Max.Y, tolerance)
                        : Near(overlap.Min.Y, self.Min.Y, tolerance);
                }
                else
                {
                    if (!TryZContact(self, other, tolerance, out overlap, out _))
                        continue;
                    onThisSide = n.Z > 0
                        ? Near(overlap.Min.Z, self.Max.Z, tolerance)
                        : Near(overlap.Min.Z, self.Min.Z, tolerance);
                }

                if (onThisSide)
                    overlaps.Add(overlap);
            }

            return overlaps;
        }


        public static bool OffsetTowardSeam(PackedContact contact, BoundingBox[] boxes, double toolDiameter, double width, out Plane placed, out bool canCut)
        {
            placed = contact.Plane;
            canCut = false;

            if (contact == null || !contact.Overlap.IsValid || toolDiameter <= 0)
                return false;

            if (!OverlapExtents(contact, out double u0, out double u1, out double v0, out double v1, out double w))
                return false;

            double pocket = Math.Max(width, 1e-6);
            double inset = toolDiameter;
            var candidates = new List<(int edge, bool seam, double halfSpan)>();

            bool[] seam = EdgeSeams(contact, boxes, u0, u1, v0, v1, w, 0.01);

            for (int e = 0; e < 4; e++)
            {
                double span = (e < 2) ? (u1 - u0) : (v1 - v0);
                candidates.Add((e, seam[e], span * 0.5));
            }

            candidates.Sort((a, b) =>
            {
                int bySeam = b.seam.CompareTo(a.seam);
                if (bySeam != 0)
                    return bySeam;
                return a.halfSpan.CompareTo(b.halfSpan);
            });

            foreach (var c in candidates)
            {
                if (!TryPlaceOnEdge(u0, u1, v0, v1, c.edge, inset, pocket, out double u, out double v))
                    continue;

                placed = PlaneAt(contact.Axis, u, v, w);
                canCut = true;
                return true;
            }

            placed = contact.Plane;
            return false;
        }


        public static bool SharesSeam(PackedContact a, PackedContact b, double tolerance)
        {
            if (a == null || b == null)
                return false;
            if (a.Axis == b.Axis)
                return false;
            if (!a.Overlap.IsValid || !b.Overlap.IsValid)
                return false;

            BoundingBox ia = a.Overlap;
            BoundingBox ib = b.Overlap;
            ia.Inflate(tolerance);
            return ia.Contains(ib.Min) || ia.Contains(ib.Max) || BoxesOverlap(a.Overlap, b.Overlap, tolerance);
        }


        public static List<PackedContact> SelectSeams(List<PackedContact> contacts, double tolerance)
        {
            var keep = new List<PackedContact>();
            for (int i = 0; i < contacts.Count; i++)
            {
                bool seam = false;
                for (int j = 0; j < contacts.Count; j++)
                {
                    if (i == j)
                        continue;
                    if (!SharesSeam(contacts[i], contacts[j], tolerance))
                        continue;
                    seam = true;
                    break;
                }

                if (seam)
                    keep.Add(contacts[i]);
            }

            return keep;
        }


        public static List<PackedContact> SelectConnected(List<PackedContact> contacts, int pieceCount, double tolerance)
        {
            var seams = new HashSet<PackedContact>(SelectSeams(contacts, tolerance));
            var ordered = new List<PackedContact>(contacts);
            ordered.Sort((a, b) =>
            {
                int ra = Rank(a, seams);
                int rb = Rank(b, seams);
                int byRank = ra.CompareTo(rb);
                if (byRank != 0)
                    return byRank;
                return b.Area.CompareTo(a.Area);
            });

            var used = new HashSet<int>();
            foreach (PackedContact c in contacts)
            {
                used.Add(c.IndexA);
                used.Add(c.IndexB);
            }

            var parent = new int[pieceCount];
            for (int i = 0; i < pieceCount; i++)
                parent[i] = i;

            int Find(int x)
            {
                if (parent[x] == x)
                    return x;
                parent[x] = Find(parent[x]);
                return parent[x];
            }

            void Union(int a, int b)
            {
                int pa = Find(a);
                int pb = Find(b);
                if (pa != pb)
                    parent[pa] = pb;
            }

            int JoinableComponents()
            {
                var roots = new HashSet<int>();
                foreach (int i in used)
                {
                    if (i >= 0 && i < pieceCount)
                        roots.Add(Find(i));
                }
                return roots.Count;
            }

            var selected = new List<PackedContact>();
            foreach (PackedContact c in ordered)
            {
                if (c.IndexA < 0 || c.IndexB < 0 || c.IndexA >= pieceCount || c.IndexB >= pieceCount)
                    continue;

                selected.Add(c);
                Union(c.IndexA, c.IndexB);
                if (used.Count > 0 && JoinableComponents() == 1)
                    break;
            }

            return selected;
        }


        public static List<List<int>> ZStacks(List<Offcut> packed, double tolerance, out List<Plane> contacts)
        {
            contacts = new List<Plane>();
            int count = packed.Count;
            var boxes = new BoundingBox[count];
            var parent = new int[count];

            for (int i = 0; i < count; i++)
            {
                boxes[i] = WorldBox(packed[i]);
                parent[i] = i;
            }

            int Find(int x)
            {
                if (parent[x] == x)
                    return x;
                parent[x] = Find(parent[x]);
                return parent[x];
            }

            void Union(int a, int b)
            {
                int pa = Find(a);
                int pb = Find(b);
                if (pa != pb)
                    parent[pa] = pb;
            }

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (!ZContact(boxes[i], boxes[j], tolerance, out Plane plane))
                        continue;

                    Union(i, j);
                    contacts.Add(plane);
                }
            }

            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < count; i++)
            {
                int root = Find(i);
                if (!groups.TryGetValue(root, out List<int> members))
                {
                    members = new List<int>();
                    groups[root] = members;
                }
                members.Add(i);
            }

            var stacks = new List<List<int>>();
            foreach (List<int> members in groups.Values)
            {
                members.Sort((a, b) => boxes[a].Min.Z.CompareTo(boxes[b].Min.Z));
                stacks.Add(members);
            }

            stacks.Sort((a, b) =>
            {
                int byZ = boxes[a[0]].Min.Z.CompareTo(boxes[b[0]].Min.Z);
                if (byZ != 0)
                    return byZ;
                return boxes[a[0]].Min.X.CompareTo(boxes[b[0]].Min.X);
            });

            return stacks;
        }


        public static List<string> ContactModes()
        {
            return new List<string>
            {
                "All",
                "Z",
                "XY",
                "Seams",
                "Connected"
            };
        }


        private static int Rank(PackedContact c, HashSet<PackedContact> seams)
        {
            if (seams.Contains(c))
                return 0;
            if (c.Axis == ContactAxis.Z)
                return 1;
            return 2;
        }


        private static PackedContact MakeContact(int i, int j, ContactAxis axis, Plane plane, BoundingBox overlap)
        {
            double area = AreaOf(overlap, axis);
            return new PackedContact
            {
                IndexA = i,
                IndexB = j,
                Axis = axis,
                Plane = plane,
                Overlap = overlap,
                Area = area
            };
        }


        private static double AreaOf(BoundingBox box, ContactAxis axis)
        {
            switch (axis)
            {
                case ContactAxis.X:
                    return Math.Abs((box.Max.Y - box.Min.Y) * (box.Max.Z - box.Min.Z));
                case ContactAxis.Y:
                    return Math.Abs((box.Max.X - box.Min.X) * (box.Max.Z - box.Min.Z));
                default:
                    return Math.Abs((box.Max.X - box.Min.X) * (box.Max.Y - box.Min.Y));
            }
        }


        private static bool TryZContact(BoundingBox a, BoundingBox b, double tolerance, out BoundingBox overlap, out Plane plane)
        {
            overlap = BoundingBox.Empty;
            plane = Plane.Unset;

            if (!a.IsValid || !b.IsValid)
                return false;
            if (!Overlap(a.Min.X, a.Max.X, b.Min.X, b.Max.X, tolerance))
                return false;
            if (!Overlap(a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y, tolerance))
                return false;

            double z;
            if (Math.Abs(a.Max.Z - b.Min.Z) <= tolerance)
                z = a.Max.Z;
            else if (Math.Abs(b.Max.Z - a.Min.Z) <= tolerance)
                z = b.Max.Z;
            else
                return false;

            double x0 = Math.Max(a.Min.X, b.Min.X);
            double x1 = Math.Min(a.Max.X, b.Max.X);
            double y0 = Math.Max(a.Min.Y, b.Min.Y);
            double y1 = Math.Min(a.Max.Y, b.Max.Y);

            overlap = new BoundingBox(new Point3d(x0, y0, z), new Point3d(x1, y1, z));
            plane = new Plane(new Point3d((x0 + x1) * 0.5, (y0 + y1) * 0.5, z), Vector3d.ZAxis);
            return true;
        }


        private static bool TryXContact(BoundingBox a, BoundingBox b, double tolerance, out BoundingBox overlap, out Plane plane)
        {
            overlap = BoundingBox.Empty;
            plane = Plane.Unset;

            if (!a.IsValid || !b.IsValid)
                return false;
            if (!Overlap(a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y, tolerance))
                return false;
            if (!Overlap(a.Min.Z, a.Max.Z, b.Min.Z, b.Max.Z, tolerance))
                return false;

            double x;
            if (Math.Abs(a.Max.X - b.Min.X) <= tolerance)
                x = a.Max.X;
            else if (Math.Abs(b.Max.X - a.Min.X) <= tolerance)
                x = b.Max.X;
            else
                return false;

            double y0 = Math.Max(a.Min.Y, b.Min.Y);
            double y1 = Math.Min(a.Max.Y, b.Max.Y);
            double z0 = Math.Max(a.Min.Z, b.Min.Z);
            double z1 = Math.Min(a.Max.Z, b.Max.Z);

            overlap = new BoundingBox(new Point3d(x, y0, z0), new Point3d(x, y1, z1));
            plane = new Plane(new Point3d(x, (y0 + y1) * 0.5, (z0 + z1) * 0.5), Vector3d.XAxis);
            return true;
        }


        private static bool TryYContact(BoundingBox a, BoundingBox b, double tolerance, out BoundingBox overlap, out Plane plane)
        {
            overlap = BoundingBox.Empty;
            plane = Plane.Unset;

            if (!a.IsValid || !b.IsValid)
                return false;
            if (!Overlap(a.Min.X, a.Max.X, b.Min.X, b.Max.X, tolerance))
                return false;
            if (!Overlap(a.Min.Z, a.Max.Z, b.Min.Z, b.Max.Z, tolerance))
                return false;

            double y;
            if (Math.Abs(a.Max.Y - b.Min.Y) <= tolerance)
                y = a.Max.Y;
            else if (Math.Abs(b.Max.Y - a.Min.Y) <= tolerance)
                y = b.Max.Y;
            else
                return false;

            double x0 = Math.Max(a.Min.X, b.Min.X);
            double x1 = Math.Min(a.Max.X, b.Max.X);
            double z0 = Math.Max(a.Min.Z, b.Min.Z);
            double z1 = Math.Min(a.Max.Z, b.Max.Z);

            overlap = new BoundingBox(new Point3d(x0, y, z0), new Point3d(x1, y, z1));
            plane = new Plane(new Point3d((x0 + x1) * 0.5, y, (z0 + z1) * 0.5), Vector3d.YAxis);
            return true;
        }


        private static bool OverlapExtents(PackedContact contact, out double u0, out double u1, out double v0, out double v1, out double w)
        {
            BoundingBox box = contact.Overlap;
            switch (contact.Axis)
            {
                case ContactAxis.X:
                    u0 = box.Min.Y;
                    u1 = box.Max.Y;
                    v0 = box.Min.Z;
                    v1 = box.Max.Z;
                    w = box.Min.X;
                    break;
                case ContactAxis.Y:
                    u0 = box.Min.X;
                    u1 = box.Max.X;
                    v0 = box.Min.Z;
                    v1 = box.Max.Z;
                    w = box.Min.Y;
                    break;
                default:
                    u0 = box.Min.X;
                    u1 = box.Max.X;
                    v0 = box.Min.Y;
                    v1 = box.Max.Y;
                    w = box.Min.Z;
                    break;
            }

            return u1 > u0 && v1 > v0;
        }


        private static Plane PlaneAt(ContactAxis axis, double u, double v, double w)
        {
            switch (axis)
            {
                case ContactAxis.X:
                    return new Plane(new Point3d(w, u, v), Vector3d.XAxis);
                case ContactAxis.Y:
                    return new Plane(new Point3d(u, w, v), Vector3d.YAxis);
                default:
                    return new Plane(new Point3d(u, v, w), Vector3d.ZAxis);
            }
        }


        private static bool TryPlaceOnEdge(double u0, double u1, double v0, double v1, int edge, double inset, double pocket, out double u, out double v)
        {
            u = (u0 + u1) * 0.5;
            v = (v0 + v1) * 0.5;
            double half = pocket * 0.5;

            switch (edge)
            {
                case 0:
                    if (u1 - u0 < inset + half)
                        return false;
                    if (v1 - v0 < pocket)
                        return false;
                    u = u0 + inset;
                    if (u - half < u0 || u + half > u1)
                        return false;
                    return true;
                case 1:
                    if (u1 - u0 < inset + half)
                        return false;
                    if (v1 - v0 < pocket)
                        return false;
                    u = u1 - inset;
                    if (u - half < u0 || u + half > u1)
                        return false;
                    return true;
                case 2:
                    if (v1 - v0 < inset + half)
                        return false;
                    if (u1 - u0 < pocket)
                        return false;
                    v = v0 + inset;
                    if (v - half < v0 || v + half > v1)
                        return false;
                    return true;
                default:
                    if (v1 - v0 < inset + half)
                        return false;
                    if (u1 - u0 < pocket)
                        return false;
                    v = v1 - inset;
                    if (v - half < v0 || v + half > v1)
                        return false;
                    return true;
            }
        }


        private static bool[] EdgeSeams(PackedContact contact, BoundingBox[] boxes, double u0, double u1, double v0, double v1, double w, double tolerance)
        {
            var seam = new bool[4];
            if (boxes == null)
                return seam;

            for (int i = 0; i < boxes.Length; i++)
            {
                if (i == contact.IndexA || i == contact.IndexB)
                    continue;

                BoundingBox c = boxes[i];
                if (!c.IsValid)
                    continue;

                switch (contact.Axis)
                {
                    case ContactAxis.Z:
                        if (TouchesX(c, u0, v0, v1, w, tolerance) && Overlap(c.Min.Y, c.Max.Y, v0, v1, 0))
                            seam[0] = true;
                        if (TouchesX(c, u1, v0, v1, w, tolerance) && Overlap(c.Min.Y, c.Max.Y, v0, v1, 0))
                            seam[1] = true;
                        if (TouchesY(c, v0, u0, u1, w, tolerance) && Overlap(c.Min.X, c.Max.X, u0, u1, 0))
                            seam[2] = true;
                        if (TouchesY(c, v1, u0, u1, w, tolerance) && Overlap(c.Min.X, c.Max.X, u0, u1, 0))
                            seam[3] = true;
                        break;
                    case ContactAxis.X:
                        if (Near(c.Min.X, w, tolerance) || Near(c.Max.X, w, tolerance))
                        {
                            if (Overlap(c.Min.Z, c.Max.Z, v0, v1, 0) && (Near(c.Min.Y, u0, tolerance) || Near(c.Max.Y, u0, tolerance)))
                                seam[0] = true;
                            if (Overlap(c.Min.Z, c.Max.Z, v0, v1, 0) && (Near(c.Min.Y, u1, tolerance) || Near(c.Max.Y, u1, tolerance)))
                                seam[1] = true;
                            if (Overlap(c.Min.Y, c.Max.Y, u0, u1, 0) && (Near(c.Min.Z, v0, tolerance) || Near(c.Max.Z, v0, tolerance)))
                                seam[2] = true;
                            if (Overlap(c.Min.Y, c.Max.Y, u0, u1, 0) && (Near(c.Min.Z, v1, tolerance) || Near(c.Max.Z, v1, tolerance)))
                                seam[3] = true;
                        }
                        break;
                    default:
                        if (Near(c.Min.Y, w, tolerance) || Near(c.Max.Y, w, tolerance))
                        {
                            if (Overlap(c.Min.Z, c.Max.Z, v0, v1, 0) && (Near(c.Min.X, u0, tolerance) || Near(c.Max.X, u0, tolerance)))
                                seam[0] = true;
                            if (Overlap(c.Min.Z, c.Max.Z, v0, v1, 0) && (Near(c.Min.X, u1, tolerance) || Near(c.Max.X, u1, tolerance)))
                                seam[1] = true;
                            if (Overlap(c.Min.X, c.Max.X, u0, u1, 0) && (Near(c.Min.Z, v0, tolerance) || Near(c.Max.Z, v0, tolerance)))
                                seam[2] = true;
                            if (Overlap(c.Min.X, c.Max.X, u0, u1, 0) && (Near(c.Min.Z, v1, tolerance) || Near(c.Max.Z, v1, tolerance)))
                                seam[3] = true;
                        }
                        break;
                }
            }

            return seam;
        }


        private static bool TouchesX(BoundingBox c, double x, double y0, double y1, double z, double tolerance)
        {
            bool onX = Near(c.Min.X, x, tolerance) || Near(c.Max.X, x, tolerance);
            bool onZ = c.Min.Z - tolerance <= z && z <= c.Max.Z + tolerance;
            return onX && onZ && Overlap(c.Min.Y, c.Max.Y, y0, y1, 0);
        }


        private static bool TouchesY(BoundingBox c, double y, double x0, double x1, double z, double tolerance)
        {
            bool onY = Near(c.Min.Y, y, tolerance) || Near(c.Max.Y, y, tolerance);
            bool onZ = c.Min.Z - tolerance <= z && z <= c.Max.Z + tolerance;
            return onY && onZ && Overlap(c.Min.X, c.Max.X, x0, x1, 0);
        }


        private static bool Near(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }


        private static bool BoxesOverlap(BoundingBox a, BoundingBox b, double tolerance)
        {
            return Overlap(a.Min.X, a.Max.X, b.Min.X, b.Max.X, -tolerance)
                && Overlap(a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y, -tolerance)
                && Overlap(a.Min.Z, a.Max.Z, b.Min.Z, b.Max.Z, -tolerance);
        }


        private static bool Overlap(double a0, double a1, double b0, double b1, double tolerance)
        {
            return a0 < b1 - tolerance && b0 < a1 - tolerance;
        }
    }
}
