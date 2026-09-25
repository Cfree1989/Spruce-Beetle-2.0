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


    /// <summary>
    /// Edge-open spline layout: Frame.X points from the mouth toward the closed stop.
    /// </summary>
    public class SplineMouth
    {
        public Plane Frame { get; set; }
        public double RunLength { get; set; }
        public double AcrossLength { get; set; }
    }


    /// <summary>
    /// Face-key layout on the packed hull. Frame origin sits on the outer face at the
    /// seam center; X along the seam, Y across the seam, Z into the wood.
    /// </summary>
    public class OutsideSeat
    {
        public Plane Frame { get; set; }
        public double SeamLength { get; set; }
        public double AcrossA { get; set; }
        public double AcrossB { get; set; }
        public double InwardA { get; set; }
        public double InwardB { get; set; }
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
        /// canCut is false when the tenon does not fit inside the overlap inset by edgeMargin on all sides.
        /// </summary>
        public static bool OrientOnOverlap(PackedContact contact, double jointX, double jointY, int tenonCount, double edgeMargin, out Plane placed, out double longSide, out double shortSide, out bool canCut)
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
            double margin = Math.Max(edgeMargin, 0);
            double usableLong = longSide - 2.0 * margin;
            double usableShort = shortSide - 2.0 * margin;
            canCut = jointX * count <= usableLong + 1e-9 && jointY <= usableShort + 1e-9;
            return true;
        }


        /// <summary>
        /// First free mouth for an edge-open spline. Prefer world +Z on vertical contacts,
        /// then either long-side end, then either short-side end. Closed end and both long
        /// edges keep <paramref name="diameter"/> of meat; the mouth is not inset.
        /// The key fills the slot, so the mouth probe is the slot length (run minus meat).
        /// </summary>
        public static bool TrySplineMouth(PackedContact contact, BoundingBox[] boxes, double jointY, int channelCount, double diameter, out SplineMouth mouth)
        {
            mouth = null;
            if (contact == null || boxes == null || !contact.Overlap.IsValid)
                return false;
            if (!OverlapExtents(contact, out double u0, out double u1, out double v0, out double v1, out double w))
                return false;

            UvWorld(contact.Axis, out Vector3d uAxis, out Vector3d vAxis, out Vector3d wAxis);
            double uLen = u1 - u0;
            double vLen = v1 - v0;
            bool uIsLong = uLen >= vLen;
            Point3d origin = OrientedPlane(contact.Axis, uIsLong, (u0 + u1) * 0.5, (v0 + v1) * 0.5, w).Origin;

            var tried = new HashSet<int>();
            bool Consider(bool runU, bool mouthMax, out SplineMouth found)
            {
                found = null;
                int key = (runU ? 1 : 0) | (mouthMax ? 2 : 0);
                if (!tried.Add(key))
                    return false;
                return EvaluateSplineMouth(contact, boxes, jointY, channelCount, diameter, runU, mouthMax, u0, u1, v0, v1, uAxis, vAxis, wAxis, origin, out found);
            }

            if (contact.Axis != ContactAxis.Z && Consider(false, true, out mouth))
                return true;

            if (uIsLong)
            {
                if (Consider(true, true, out mouth)) return true;
                if (Consider(true, false, out mouth)) return true;
                if (Consider(false, true, out mouth)) return true;
                if (Consider(false, false, out mouth)) return true;
            }
            else
            {
                if (Consider(false, true, out mouth)) return true;
                if (Consider(false, false, out mouth)) return true;
                if (Consider(true, true, out mouth)) return true;
                if (Consider(true, false, out mouth)) return true;
            }

            mouth = null;
            return false;
        }


        /// <summary>
        /// Vertical hull faces where both members of <paramref name="contact"/> are
        /// coplanar with the packed AABB. Stepped and interior seams yield an empty list.
        /// One contact can seat on more than one side.
        /// </summary>
        public static bool TryOutsideSeats(PackedContact contact, BoundingBox[] boxes, out List<OutsideSeat> seats)
        {
            seats = new List<OutsideSeat>();
            if (contact == null || boxes == null || !contact.Overlap.IsValid)
                return false;
            if (contact.IndexA < 0 || contact.IndexB < 0 || contact.IndexA >= boxes.Length || contact.IndexB >= boxes.Length)
                return false;

            BoundingBox hull = UnionHull(boxes);
            if (!hull.IsValid)
                return false;

            BoundingBox a = boxes[contact.IndexA];
            BoundingBox b = boxes[contact.IndexB];
            if (!a.IsValid || !b.IsValid)
                return false;

            const double tol = 0.01;
            if (!OverlapExtents(contact, out _, out _, out _, out _, out double w))
                return false;

            TryAddOutsideSeat(seats, contact, a, b, hull, 0, true, w, tol);
            TryAddOutsideSeat(seats, contact, a, b, hull, 0, false, w, tol);
            TryAddOutsideSeat(seats, contact, a, b, hull, 1, true, w, tol);
            TryAddOutsideSeat(seats, contact, a, b, hull, 1, false, w, tol);
            return seats.Count > 0;
        }


        private static void TryAddOutsideSeat(
            List<OutsideSeat> seats,
            PackedContact contact,
            BoundingBox a,
            BoundingBox b,
            BoundingBox hull,
            int faceAxis,
            bool facePositive,
            double contactW,
            double tol)
        {
            int contactAxis = (int)contact.Axis;
            if (contactAxis == faceAxis)
                return;

            if (!OnVertFace(a, hull, faceAxis, facePositive, tol) || !OnVertFace(b, hull, faceAxis, facePositive, tol))
                return;
            if (!OnVertFace(contact.Overlap, hull, faceAxis, facePositive, tol))
                return;

            int seamAxis = 3 - faceAxis - contactAxis;
            if (seamAxis < 0 || seamAxis > 2)
                return;

            AxisRange(contact.Overlap, seamAxis, out double seam0, out double seam1);
            double seamLen = seam1 - seam0;
            if (seamLen <= tol)
                return;

            double faceCoord = facePositive ? MaxCoord(hull, faceAxis) : MinCoord(hull, faceAxis);
            var origin = new Point3d();
            SetCoord(ref origin, faceAxis, faceCoord);
            SetCoord(ref origin, contactAxis, contactW);
            SetCoord(ref origin, seamAxis, (seam0 + seam1) * 0.5);

            Vector3d xDir = WorldAxis(seamAxis);
            Vector3d inward = facePositive ? -WorldAxis(faceAxis) : WorldAxis(faceAxis);
            Vector3d yDir = Vector3d.CrossProduct(inward, xDir);
            if (!xDir.Unitize() || !yDir.Unitize() || !inward.Unitize())
                return;

            var frame = new Plane(origin, xDir, yDir);
            if (!frame.IsValid)
                return;
            if (frame.ZAxis * inward < 0)
                frame.Flip();

            double acrossA = AcrossFromContact(a, contact.Axis, contactW, tol);
            double acrossB = AcrossFromContact(b, contact.Axis, contactW, tol);
            if (acrossA <= tol || acrossB <= tol)
                return;

            seats.Add(new OutsideSeat
            {
                Frame = frame,
                SeamLength = seamLen,
                AcrossA = acrossA,
                AcrossB = acrossB,
                InwardA = ThicknessOnAxis(a, faceAxis),
                InwardB = ThicknessOnAxis(b, faceAxis)
            });
        }


        private static BoundingBox UnionHull(BoundingBox[] boxes)
        {
            var hull = BoundingBox.Empty;
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i].IsValid)
                    hull.Union(boxes[i]);
            }
            return hull;
        }


        private static bool OnVertFace(BoundingBox box, BoundingBox hull, int axis, bool positive, double tol)
        {
            if (!box.IsValid)
                return false;
            double face = positive ? MaxCoord(hull, axis) : MinCoord(hull, axis);
            double edge = positive ? MaxCoord(box, axis) : MinCoord(box, axis);
            return Near(edge, face, tol);
        }


        private static double AcrossFromContact(BoundingBox box, ContactAxis axis, double w, double tolerance)
        {
            switch (axis)
            {
                case ContactAxis.X:
                    return Near(box.Max.X, w, tolerance) ? w - box.Min.X : box.Max.X - w;
                case ContactAxis.Y:
                    return Near(box.Max.Y, w, tolerance) ? w - box.Min.Y : box.Max.Y - w;
                default:
                    return Near(box.Max.Z, w, tolerance) ? w - box.Min.Z : box.Max.Z - w;
            }
        }


        private static double ThicknessOnAxis(BoundingBox box, int axis)
        {
            return MaxCoord(box, axis) - MinCoord(box, axis);
        }


        private static void AxisRange(BoundingBox box, int axis, out double min, out double max)
        {
            min = MinCoord(box, axis);
            max = MaxCoord(box, axis);
        }


        private static double MinCoord(BoundingBox box, int axis)
        {
            switch (axis)
            {
                case 0: return box.Min.X;
                case 1: return box.Min.Y;
                default: return box.Min.Z;
            }
        }


        private static double MaxCoord(BoundingBox box, int axis)
        {
            switch (axis)
            {
                case 0: return box.Max.X;
                case 1: return box.Max.Y;
                default: return box.Max.Z;
            }
        }


        private static void SetCoord(ref Point3d point, int axis, double value)
        {
            switch (axis)
            {
                case 0: point.X = value; break;
                case 1: point.Y = value; break;
                default: point.Z = value; break;
            }
        }


        private static Vector3d WorldAxis(int axis)
        {
            switch (axis)
            {
                case 0: return Vector3d.XAxis;
                case 1: return Vector3d.YAxis;
                default: return Vector3d.ZAxis;
            }
        }


        private static bool EvaluateSplineMouth(
            PackedContact contact,
            BoundingBox[] boxes,
            double jointY,
            int channelCount,
            double diameter,
            bool runU,
            bool mouthMax,
            double u0, double u1, double v0, double v1,
            Vector3d uAxis, Vector3d vAxis, Vector3d wAxis,
            Point3d origin,
            out SplineMouth mouth)
        {
            mouth = null;
            double runLen = runU ? (u1 - u0) : (v1 - v0);
            double acrLen = runU ? (v1 - v0) : (u1 - u0);
            Vector3d runAxis = runU ? uAxis : vAxis;
            int count = Math.Max(channelCount, 1);
            double meat = Math.Max(diameter, 0);
            double keyLength = runLen - meat;

            if (jointY * count > acrLen - 2.0 * meat + 1e-9)
                return false;
            if (keyLength <= 1e-9 || acrLen <= 2.0 * meat + 1e-9)
                return false;

            Vector3d outDir = mouthMax ? runAxis : -runAxis;
            Vector3d drive = -outDir;
            BoundingBox probe = MouthProbe(contact.Overlap, contact.Axis, outDir, keyLength);
            if (ProbeHitsOther(probe, boxes, contact.IndexA, contact.IndexB))
                return false;

            Vector3d across = Vector3d.CrossProduct(wAxis, drive);
            if (!across.Unitize() || !drive.Unitize())
                return false;

            var frame = new Plane(origin, drive, across);
            if (!frame.IsValid)
                return false;

            mouth = new SplineMouth
            {
                Frame = frame,
                RunLength = runLen,
                AcrossLength = acrLen
            };
            return true;
        }


        private static void UvWorld(ContactAxis axis, out Vector3d uAxis, out Vector3d vAxis, out Vector3d wAxis)
        {
            switch (axis)
            {
                case ContactAxis.X:
                    uAxis = Vector3d.YAxis;
                    vAxis = Vector3d.ZAxis;
                    wAxis = Vector3d.XAxis;
                    break;
                case ContactAxis.Y:
                    uAxis = Vector3d.XAxis;
                    vAxis = Vector3d.ZAxis;
                    wAxis = Vector3d.YAxis;
                    break;
                default:
                    uAxis = Vector3d.XAxis;
                    vAxis = Vector3d.YAxis;
                    wAxis = Vector3d.ZAxis;
                    break;
            }
        }


        private static BoundingBox MouthProbe(BoundingBox overlap, ContactAxis axis, Vector3d outDir, double length)
        {
            const double hair = 1e-4;
            const double inflate = 0.01;
            Point3d min = overlap.Min;
            Point3d max = overlap.Max;

            switch (axis)
            {
                case ContactAxis.X:
                    min.X -= inflate;
                    max.X += inflate;
                    break;
                case ContactAxis.Y:
                    min.Y -= inflate;
                    max.Y += inflate;
                    break;
                default:
                    min.Z -= inflate;
                    max.Z += inflate;
                    break;
            }

            if (outDir.Z > 0.5)
            {
                min.Z = overlap.Max.Z + hair;
                max.Z = overlap.Max.Z + hair + length;
            }
            else if (outDir.Z < -0.5)
            {
                max.Z = overlap.Min.Z - hair;
                min.Z = overlap.Min.Z - hair - length;
            }
            else if (outDir.Y > 0.5)
            {
                min.Y = overlap.Max.Y + hair;
                max.Y = overlap.Max.Y + hair + length;
            }
            else if (outDir.Y < -0.5)
            {
                max.Y = overlap.Min.Y - hair;
                min.Y = overlap.Min.Y - hair - length;
            }
            else if (outDir.X > 0.5)
            {
                min.X = overlap.Max.X + hair;
                max.X = overlap.Max.X + hair + length;
            }
            else
            {
                max.X = overlap.Min.X - hair;
                min.X = overlap.Min.X - hair - length;
            }

            return new BoundingBox(min, max);
        }


        private static bool ProbeHitsOther(BoundingBox probe, BoundingBox[] boxes, int skipA, int skipB)
        {
            if (!probe.IsValid)
                return true;

            for (int i = 0; i < boxes.Length; i++)
            {
                if (i == skipA || i == skipB)
                    continue;
                BoundingBox other = boxes[i];
                if (!other.IsValid)
                    continue;
                if (probe.Min.X <= other.Max.X && probe.Max.X >= other.Min.X
                    && probe.Min.Y <= other.Max.Y && probe.Max.Y >= other.Min.Y
                    && probe.Min.Z <= other.Max.Z && probe.Max.Z >= other.Min.Z)
                    return true;
            }

            return false;
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
