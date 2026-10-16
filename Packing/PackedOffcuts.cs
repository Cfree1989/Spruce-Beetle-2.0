using System.Collections.Generic;
using System.Globalization;
using CromulentBisgetti.ContainerPacking.Entities;
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    /// <summary>
    /// Turns packed library items into origin-aligned Offcut solids. Bin Packing, Stagger Column, and Spine Column share this.
    /// </summary>
    static class PackedOffcuts
    {
        public static Box OriginBox(Box boundingBox)
        {
            var originBox = new Box(Plane.WorldXY,
                new Interval(0, boundingBox.X.Length),
                new Interval(0, boundingBox.Y.Length),
                new Interval(0, boundingBox.Z.Length));

            /*  Because kinked surfaces can cause problems down stream, Rhino always splits kinked surfaces when adding Breps to the document.
                Sometimes, we have to do it.    */
            originBox.ToBrep().Faces.SplitKinkyFaces(0.0001);
            return originBox;
        }


        public static Container MakeContainer(Box boundingBox)
        {
            return new Container(0,
                (decimal)boundingBox.X.Length,
                (decimal)boundingBox.Y.Length,
                (decimal)boundingBox.Z.Length);
        }


        public static List<Item> MakeItems(List<Offcut> offcutData)
        {
            var packItems = new List<Item>(offcutData.Count);
            for (int i = 0; i < offcutData.Count; i++)
            {
                double xI = offcutData[i].X;
                decimal x = (decimal)xI;

                double yI = offcutData[i].Y;
                decimal y = (decimal)yI;

                double zI = offcutData[i].Z;
                decimal z = (decimal)zI;

                packItems.Add(new Item(i, x, y, z, 1));
            }

            return packItems;
        }


        public static List<Offcut> FromItems(List<Item> packedItems, List<Offcut> offcutData)
        {
            var packedOffcuts = new List<Offcut>(packedItems.Count);

            // EB-AFIT is a pallet packer: Length→X, Height→Y, Width→Z.
            // Rhino/architecture uses Z-up, so swap library Y (height) with Z (width).
            for (int i = 0; i < packedItems.Count; i++)
            {
                double ocX = (double)packedItems[i].PackDimX;
                double ocY = (double)packedItems[i].PackDimZ;
                double ocZ = (double)packedItems[i].PackDimY;

                double cX = (double)packedItems[i].CoordX;
                double cY = (double)packedItems[i].CoordZ;
                double cZ = (double)packedItems[i].CoordY;

                Point3d basePt = new Point3d(cX, cY, cZ);
                Vector3d normalVec = new Vector3d(0, 0, ocZ);

                Plane basePlane = new Plane(basePt, Vector3d.ZAxis);

                Rectangle3d baseRect = new Rectangle3d(basePlane, ocX, ocY);

                Brep openOffcut = Surface.CreateExtrusion(baseRect.ToNurbsCurve(), normalVec).ToBrep();
                Brep closedOffcut = openOffcut.CapPlanarHoles(0.0001);

                /*  Because kinked surfaces can cause problems down stream, Rhino always splits kinked surfaces when adding Breps to the document.
                    Sometimes, we have to do it.    */
                closedOffcut.Faces.SplitKinkyFaces(0.0001);

                if (BrepSolidOrientation.Inward == closedOffcut.SolidOrientation)
                    closedOffcut.Flip();

                int sourceId = packedItems[i].ID;
                Offcut source = (sourceId >= 0 && sourceId < offcutData.Count)
                    ? offcutData[sourceId]
                    : new Offcut(sourceId, ocX, ocY, ocZ);

                Point3d bottom = new Point3d(cX + ocX * 0.5, cY + ocY * 0.5, cZ);
                Point3d top = new Point3d(cX + ocX * 0.5, cY + ocY * 0.5, cZ + ocZ);
                Point3d mid = new Point3d(cX + ocX * 0.5, cY + ocY * 0.5, cZ + ocZ * 0.5);

                Plane firstPlane = new Plane(bottom, Vector3d.ZAxis);
                Plane secondPlane = new Plane(top, Vector3d.ZAxis);
                Plane averagePlane = new Plane(mid, Vector3d.ZAxis);

                packedOffcuts.Add(Offcut.CreateOffcut(
                    closedOffcut,
                    source.Index,
                    ocX,
                    ocY,
                    ocZ,
                    source.Vol > 0 ? source.Vol : ocX * ocY * ocZ,
                    ocX * ocY * ocZ,
                    firstPlane,
                    secondPlane,
                    averagePlane,
                    averagePlane,
                    basePlane,
                    0));
            }

            return packedOffcuts;
        }


        public static void Write(IGH_DataAccess access, Box originBox, List<Offcut> packedOffcuts)
        {
            var packedBreps = new List<Brep>(packedOffcuts.Count);
            var packedGH = new List<Offcut_GH>(packedOffcuts.Count);
            for (int i = 0; i < packedOffcuts.Count; i++)
            {
                packedBreps.Add(packedOffcuts[i].OffcutGeometry);
                packedGH.Add(new Offcut_GH(packedOffcuts[i]));
            }

            access.SetDataList(0, packedBreps);
            access.SetData(1, originBox);
            access.SetDataList(2, packedGH);
        }


        public static string Inches(decimal value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }


        public static string Inches(IReadOnlyList<decimal> values)
        {
            if (values == null || values.Count == 0)
                return "none";

            var parts = new string[values.Count];
            for (int i = 0; i < values.Count; i++)
                parts[i] = Inches(values[i]);
            return string.Join(", ", parts);
        }
    }
}
