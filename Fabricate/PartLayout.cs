using System;
using System.Collections.Generic;
using Rhino.Geometry;


namespace SpruceBeetle.Fabricate
{
    /// <summary>
    /// Squares a solid to a corner frame (longest edge on Y, thinnest on Z) and places those frames on a base plane.
    /// </summary>
    static class PartLayout
    {
        public sealed class Piece
        {
            public Brep Geometry;
            public Offcut Source;
            public Plane SourceCorner;
            public double Length;
            public double Width;
            public double Thickness;
            public bool UsedWorldBox;
            public int InputOrder;
            public double ScrapNumber;
            public bool HasNumber;
        }


        public static bool TryMeasure(Brep brep, Plane stockPlane, bool hasStockPlane, double tolerance, out Piece piece)
        {
            piece = null;
            if (brep == null)
                return false;

            double tol = tolerance > 0 ? tolerance : 0.001;
            BoundingBox world = brep.GetBoundingBox(true);
            double worldVolume = BoxVolume(world);
            if (worldVolume <= tol * tol * tol)
                return false;

            bool usedWorld = false;
            Plane corner = Plane.Unset;
            double length = 0, width = 0, thickness = 0;
            bool measured = false;

            if (TryFaceFrame(brep, tol, out Plane faceCorner, out double faceL, out double faceW, out double faceT)
                && faceL * faceW * faceT <= worldVolume * 1.25)
            {
                corner = faceCorner;
                length = faceL;
                width = faceW;
                thickness = faceT;
                measured = true;
            }
            else if (hasStockPlane
                && TryPlaneFrame(brep, stockPlane, tol, out Plane stockCorner, out double stockL, out double stockW, out double stockT)
                && stockL * stockW * stockT <= worldVolume * 1.25)
            {
                corner = stockCorner;
                length = stockL;
                width = stockW;
                thickness = stockT;
                measured = true;
            }
            else if (TryPlaneFrame(brep, Plane.WorldXY, tol, out Plane worldCorner, out double worldL, out double worldW, out double worldT))
            {
                corner = worldCorner;
                length = worldL;
                width = worldW;
                thickness = worldT;
                usedWorld = true;
                measured = true;
            }

            if (!measured || length <= tol)
                return false;

            piece = new Piece
            {
                Geometry = brep,
                SourceCorner = corner,
                Length = length,
                Width = width,
                Thickness = thickness,
                UsedWorldBox = usedWorld
            };
            return true;
        }


        public static double AutomaticGap(IReadOnlyList<Piece> pieces)
        {
            double thickest = 0;
            double longest = 0;
            if (pieces != null)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i] == null)
                        continue;
                    if (pieces[i].Thickness > thickest)
                        thickest = pieces[i].Thickness;
                    if (pieces[i].Length > longest)
                        longest = pieces[i].Length;
                }
            }

            if (thickest > 1e-6)
                return thickest;
            return longest * 0.1;
        }


        public static void SortByNumber(List<Piece> pieces)
        {
            if (pieces == null || pieces.Count < 2)
                return;

            pieces.Sort(CompareByNumber);
        }


        static int CompareByNumber(Piece a, Piece b)
        {
            bool aNumbered = a != null && a.HasNumber;
            bool bNumbered = b != null && b.HasNumber;
            if (aNumbered && bNumbered)
            {
                int byIndex = a.ScrapNumber.CompareTo(b.ScrapNumber);
                if (byIndex != 0)
                    return byIndex;
            }
            else if (aNumbered != bNumbered)
            {
                return aNumbered ? -1 : 1;
            }

            int orderA = a?.InputOrder ?? 0;
            int orderB = b?.InputOrder ?? 0;
            return orderA.CompareTo(orderB);
        }


        public static void Place(IReadOnlyList<Piece> pieces, Plane basis, bool line, double gap,
            List<Brep> breps, List<Offcut> offcuts, List<Plane> frames, List<Piece> placed)
        {
            basis = Orthonormal(basis);
            if (gap < 0 || double.IsNaN(gap) || double.IsInfinity(gap))
                gap = 0;

            double cursor = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                Piece piece = pieces[i];
                if (piece?.Geometry == null)
                    continue;

                Plane target = basis;
                target.Origin = basis.Origin + basis.XAxis * cursor;

                Transform xform = Transform.PlaneToPlane(piece.SourceCorner, target);
                Brep copy = piece.Geometry.DuplicateBrep();
                if (copy == null || !copy.Transform(xform))
                    continue;

                breps.Add(copy);
                frames.Add(target);
                offcuts.Add(piece.Source != null
                    ? MoveOffcut(piece.Source, copy, xform)
                    : OffcutFromPlacement(copy, target, piece, piece.HasNumber ? piece.ScrapNumber : i));
                placed?.Add(piece);

                if (line)
                    cursor += piece.Width + gap;
            }
        }


        static bool TryFaceFrame(Brep brep, double tolerance, out Plane corner, out double length, out double width, out double thickness)
        {
            corner = Plane.Unset;
            length = width = thickness = 0;

            var clusters = new List<AxisCluster>();
            double parallel = Math.Cos(1.0 * Math.PI / 180.0);
            foreach (BrepFace face in brep.Faces)
            {
                if (face == null || !face.TryGetPlane(out Plane facePlane, tolerance))
                    continue;

                Brep faceBrep = face.DuplicateFace(false);
                if (faceBrep == null)
                    continue;

                AreaMassProperties area = AreaMassProperties.Compute(faceBrep);
                faceBrep.Dispose();
                if (area == null || area.Area <= tolerance * tolerance)
                    continue;

                Vector3d normal = facePlane.Normal;
                if (!normal.Unitize())
                    continue;

                bool merged = false;
                for (int i = 0; i < clusters.Count; i++)
                {
                    if (Math.Abs(clusters[i].Direction * normal) >= parallel)
                    {
                        clusters[i].Area += area.Area;
                        merged = true;
                        break;
                    }
                }

                if (!merged)
                    clusters.Add(new AxisCluster { Direction = normal, Area = area.Area });
            }

            if (clusters.Count < 2)
                return false;

            clusters.Sort((a, b) => b.Area.CompareTo(a.Area));
            double perpendicular = Math.Sin(5.0 * Math.PI / 180.0);
            Vector3d axisA = Vector3d.Unset;
            Vector3d axisB = Vector3d.Unset;
            bool found = false;
            for (int i = 0; i < clusters.Count && !found; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    if (Math.Abs(clusters[i].Direction * clusters[j].Direction) >= perpendicular)
                        continue;
                    axisA = clusters[i].Direction;
                    axisB = clusters[j].Direction;
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;

            return TryAxes(brep, axisA, axisB, tolerance, out corner, out length, out width, out thickness);
        }


        static bool TryPlaneFrame(Brep brep, Plane stock, double tolerance, out Plane corner, out double length, out double width, out double thickness)
        {
            corner = Plane.Unset;
            length = width = thickness = 0;
            if (!stock.IsValid)
                return false;
            return TryAxes(brep, stock.XAxis, stock.YAxis, tolerance, out corner, out length, out width, out thickness);
        }


        static bool TryAxes(Brep brep, Vector3d axisA, Vector3d axisB, double tolerance, out Plane corner, out double length, out double width, out double thickness)
        {
            corner = Plane.Unset;
            length = width = thickness = 0;

            if (!axisA.Unitize() || !axisB.Unitize())
                return false;

            Vector3d axisC = Vector3d.CrossProduct(axisA, axisB);
            if (!axisC.Unitize())
                return false;
            axisB = Vector3d.CrossProduct(axisC, axisA);
            if (!axisB.Unitize())
                return false;

            Plane measure = new Plane(Point3d.Origin, axisA, axisB);
            BoundingBox box = brep.GetBoundingBox(measure);
            double sizeX = box.Max.X - box.Min.X;
            double sizeY = box.Max.Y - box.Min.Y;
            double sizeZ = box.Max.Z - box.Min.Z;
            if (sizeX < 0 || sizeY < 0 || sizeZ < 0)
                return false;

            var axes = new List<SizedAxis>
            {
                new SizedAxis { Direction = measure.XAxis, Size = sizeX, Id = 0 },
                new SizedAxis { Direction = measure.YAxis, Size = sizeY, Id = 1 },
                new SizedAxis { Direction = measure.ZAxis, Size = sizeZ, Id = 2 }
            };
            axes.Sort((a, b) =>
            {
                double delta = Math.Abs(a.Size - b.Size);
                if (delta <= tolerance)
                    return a.Id.CompareTo(b.Id);
                return b.Size.CompareTo(a.Size);
            });

            Vector3d xDir = axes[1].Direction;
            Vector3d yDir = axes[0].Direction;
            Vector3d zDir = axes[2].Direction;
            if (Vector3d.CrossProduct(xDir, yDir) * zDir < 0)
                zDir = -zDir;

            length = axes[0].Size;
            width = axes[1].Size;
            thickness = axes[2].Size;
            if (length <= tolerance)
                return false;

            Point3d origin = measure.Origin
                + measure.XAxis * LowCoord(measure.XAxis, box.Min.X, box.Max.X, xDir, yDir, zDir)
                + measure.YAxis * LowCoord(measure.YAxis, box.Min.Y, box.Max.Y, xDir, yDir, zDir)
                + measure.ZAxis * LowCoord(measure.ZAxis, box.Min.Z, box.Max.Z, xDir, yDir, zDir);

            corner = new Plane(origin, xDir, yDir);
            return corner.IsValid;
        }


        static double LowCoord(Vector3d measureAxis, double min, double max, Vector3d xDir, Vector3d yDir, Vector3d zDir)
        {
            if (Math.Abs(xDir * measureAxis) > 0.5)
                return xDir * measureAxis > 0 ? min : max;
            if (Math.Abs(yDir * measureAxis) > 0.5)
                return yDir * measureAxis > 0 ? min : max;
            return zDir * measureAxis > 0 ? min : max;
        }


        static double BoxVolume(BoundingBox box)
        {
            if (!box.IsValid)
                return 0;
            double x = box.Max.X - box.Min.X;
            double y = box.Max.Y - box.Min.Y;
            double z = box.Max.Z - box.Min.Z;
            if (x <= 0 || y <= 0 || z <= 0)
                return 0;
            return x * y * z;
        }


        static Plane Orthonormal(Plane basis)
        {
            if (!basis.IsValid)
                return Plane.WorldXY;

            Vector3d x = basis.XAxis;
            Vector3d y = basis.YAxis;
            if (!x.Unitize() || !y.Unitize())
                return Plane.WorldXY;

            Vector3d z = Vector3d.CrossProduct(x, y);
            if (!z.Unitize())
                return Plane.WorldXY;

            y = Vector3d.CrossProduct(z, x);
            if (!y.Unitize())
                return Plane.WorldXY;

            return new Plane(basis.Origin, x, y);
        }


        static Offcut MoveOffcut(Offcut source, Brep geometry, Transform xform)
        {
            Offcut moved = new Offcut(source)
            {
                OffcutGeometry = geometry,
                FirstPlane = MovePlane(source.FirstPlane, xform),
                SecondPlane = MovePlane(source.SecondPlane, xform),
                AveragePlane = MovePlane(source.AveragePlane, xform),
                MovedAveragePlane = MovePlane(source.MovedAveragePlane, xform),
                BasePlane = MovePlane(source.BasePlane, xform)
            };
            return moved;
        }


        static Plane MovePlane(Plane plane, Transform xform)
        {
            if (!plane.IsValid)
                return plane;
            Plane copy = new Plane(plane);
            copy.Transform(xform);
            return copy;
        }


        static Offcut OffcutFromPlacement(Brep geometry, Plane corner, Piece piece, double index)
        {
            Plane top = corner;
            top.Origin = corner.Origin + corner.ZAxis * piece.Thickness;
            Plane mid = corner;
            mid.Origin = corner.Origin + corner.ZAxis * (piece.Thickness * 0.5);
            double volume = piece.Length * piece.Width * Math.Max(piece.Thickness, 0);
            return Offcut.CreateOffcut(geometry, index, piece.Width, piece.Length, piece.Thickness, volume, volume,
                corner, top, mid, mid, corner, 0);
        }


        sealed class AxisCluster
        {
            public Vector3d Direction;
            public double Area;
        }


        struct SizedAxis
        {
            public Vector3d Direction;
            public double Size;
            public int Id;
        }
    }
}
