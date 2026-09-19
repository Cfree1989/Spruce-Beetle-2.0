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


        /// <summary>
        /// Tenon frame at the overlap-rectangle center, X along the longer in-plane side.
        /// canCut is false when JX × count exceeds the long side or JY exceeds the short side.
        /// </summary>
        public static bool OrientOnOverlap(PackedContact contact, double jointX, double jointY, int tenonCount, out Plane placed, out double longSide, out double shortSide, out bool canCut)
        {
            placed = Plane.Unset;
            longSide = 0;
            shortSide = 0;
            canCut = false;

            if (contact == null || !contact.Overlap.IsValid)
                return false;

            if (!OverlapExtents(contact, out double u0, out double u1, out double v0, out double v1, out double w))
                return false;

            double uLen = u1 - u0;
            double vLen = v1 - v0;
            double uc = (u0 + u1) * 0.5;
            double vc = (v0 + v1) * 0.5;
            bool uIsLong = uLen >= vLen;
            longSide = uIsLong ? uLen : vLen;
            shortSide = uIsLong ? vLen : uLen;
            placed = OrientedPlane(contact.Axis, uIsLong, uc, vc, w);

            int count = Math.Max(tenonCount, 1);
            canCut = jointX * count <= longSide + 1e-9 && jointY <= shortSide + 1e-9;
            return true;
        }


        public static List<string> ContactModes()
        {
            return new List<string>
            {
                "All",
                "Z",
                "XY"
            };
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


        private static Plane OrientedPlane(ContactAxis axis, bool uIsLong, double u, double v, double w)
        {
            switch (axis)
            {
                case ContactAxis.X:
                    return uIsLong
                        ? new Plane(new Point3d(w, u, v), Vector3d.YAxis, Vector3d.ZAxis)
                        : new Plane(new Point3d(w, u, v), Vector3d.ZAxis, -Vector3d.YAxis);
                case ContactAxis.Y:
                    return uIsLong
                        ? new Plane(new Point3d(u, w, v), Vector3d.XAxis, -Vector3d.ZAxis)
                        : new Plane(new Point3d(u, w, v), Vector3d.ZAxis, Vector3d.XAxis);
                default:
                    return uIsLong
                        ? new Plane(new Point3d(u, v, w), Vector3d.XAxis, Vector3d.YAxis)
                        : new Plane(new Point3d(u, v, w), Vector3d.YAxis, -Vector3d.XAxis);
            }
        }


        private static bool Near(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) <= tolerance;
        }


        private static bool Overlap(double a0, double a1, double b0, double b1, double tolerance)
        {
            return a0 < b1 - tolerance && b0 < a1 - tolerance;
        }
    }
}
