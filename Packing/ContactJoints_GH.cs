using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class ContactJoints_GH : GH_Component
    {
        public ContactJoints_GH()
          : base("Contact Joints", "PackJoints",
              "Cut tool-sized pockets at packed face contacts, offset toward a seam rather than the face center",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed Offcuts (same list as Packed Contacts)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Contacts", "C", "Selected contacts from Select Contacts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tool Diameter", "D", "CNC bit diameter (joint width, depth cap, fillet, and edge inset)", GH_ParamAccess.item, 0.25);
            pManager.AddNumberParameter("Width Factor", "W", "Pocket width as a multiple of D", GH_ParamAccess.item, 1.0);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts after pocket cuts", GH_ParamAccess.list);
            pManager.AddBrepParameter("Joints", "J", "Pocket solids (matching keys / splines)", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Skipped", "Sk", "Contact planes skipped because the overlap is too small for D", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var packed = new List<Offcut>();
            var objs = new List<object>();
            double diameter = 0.25;
            double widthFactor = 1.0;

            if (!DA.GetDataList(0, packed))
                return;
            if (!DA.GetDataList(1, objs))
                return;
            DA.GetData(2, ref diameter);
            DA.GetData(3, ref widthFactor);

            if (packed.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No packed Offcuts provided.");
                return;
            }

            if (diameter <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tool diameter must be greater than 0.");
                return;
            }

            if (widthFactor <= 0)
                widthFactor = 1.0;

            double pocketWidth = diameter * widthFactor;
            var boxes = new BoundingBox[packed.Count];
            var geometry = new Brep[packed.Count];
            for (int i = 0; i < packed.Count; i++)
            {
                boxes[i] = PackedNeighbors.WorldBox(packed[i]);
                geometry[i] = packed[i].OffcutGeometry?.DuplicateBrep();
            }

            var cutters = new List<Brep>();
            var skipped = new List<Plane>();
            int cutCount = 0;
            int failCount = 0;

            for (int i = 0; i < objs.Count; i++)
            {
                PackedContact contact = PackedContact_GH.Parse(objs[i]);
                if (contact == null)
                    continue;
                if (contact.IndexA < 0 || contact.IndexB < 0 || contact.IndexA >= packed.Count || contact.IndexB >= packed.Count)
                {
                    skipped.Add(contact.Plane);
                    continue;
                }

                if (!PackedNeighbors.OffsetTowardSeam(contact, boxes, diameter, pocketWidth, out Plane placed, out bool canCut) || !canCut)
                {
                    skipped.Add(contact.Plane);
                    continue;
                }

                double depth = PocketDepth(boxes[contact.IndexA], boxes[contact.IndexB], contact.Axis, diameter);
                if (depth <= 0)
                {
                    skipped.Add(contact.Plane);
                    continue;
                }

                if (!TryMakePocket(placed, pocketWidth, depth, diameter, out Brep cutter))
                {
                    skipped.Add(placed);
                    continue;
                }

                cutters.Add(cutter);
                bool okA = TryCut(geometry[contact.IndexA], cutter, out geometry[contact.IndexA]);
                bool okB = TryCut(geometry[contact.IndexB], cutter, out geometry[contact.IndexB]);
                if (okA && okB)
                    cutCount++;
                else
                {
                    failCount++;
                    skipped.Add(placed);
                }
            }

            var output = new List<Offcut_GH>(packed.Count);
            for (int i = 0; i < packed.Count; i++)
            {
                Offcut copy = packed[i].Duplicate();
                if (geometry[i] != null)
                {
                    copy.OffcutGeometry = geometry[i];
                    try
                    {
                        copy.FabVol = geometry[i].GetVolume(0.0001, 0.0001);
                    }
                    catch
                    {
                    }
                }
                output.Add(new Offcut_GH(copy));
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{cutCount} pocket(s) cut, {skipped.Count} skipped, {failCount} boolean fail(s).");

            DA.SetDataList(0, output);
            DA.SetDataList(1, cutters);
            DA.SetDataList(2, skipped);
        }


        private static double PocketDepth(BoundingBox a, BoundingBox b, ContactAxis axis, double diameter)
        {
            double thickA;
            double thickB;
            switch (axis)
            {
                case ContactAxis.X:
                    thickA = a.Max.X - a.Min.X;
                    thickB = b.Max.X - b.Min.X;
                    break;
                case ContactAxis.Y:
                    thickA = a.Max.Y - a.Min.Y;
                    thickB = b.Max.Y - b.Min.Y;
                    break;
                default:
                    thickA = a.Max.Z - a.Min.Z;
                    thickB = b.Max.Z - b.Min.Z;
                    break;
            }

            double cap = Math.Min(thickA, thickB) / 3.0;
            return Math.Min(diameter, cap);
        }


        private static bool TryMakePocket(Plane plane, double width, double depth, double diameter, out Brep pocket)
        {
            pocket = null;
            Plane basePlane = plane;
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * depth * 0.5));

            var rect = new Rectangle3d(basePlane, new Interval(-width * 0.5, width * 0.5), new Interval(-width * 0.5, width * 0.5));
            Curve outline = rect.ToNurbsCurve();
            double fillet = Math.Min(diameter * 0.5, width * 0.5 - 1e-4);
            if (fillet > 1e-6)
            {
                Curve filleted = Curve.CreateFilletCornersCurve(outline, fillet, 0.0001, 0.0001);
                if (filleted != null)
                    outline = filleted;
            }

            Brep extrude = Extrusion.Create(outline, depth, true)?.ToBrep();
            if (extrude == null)
                return false;

            extrude.Faces.SplitKinkyFaces(0.0001);
            if (BrepSolidOrientation.Inward == extrude.SolidOrientation)
                extrude.Flip();

            pocket = extrude;
            return true;
        }


        private static bool TryCut(Brep solid, Brep cutter, out Brep result)
        {
            result = solid;
            if (solid == null || cutter == null)
                return false;

            Brep[] cut = Brep.CreateBooleanDifference(new[] { solid }, new[] { cutter }, 0.0001);
            if (cut == null || cut.Length == 0 || cut[0] == null)
                return false;

            Brep body = cut[0];
            body.Faces.SplitKinkyFaces(0.0001);
            body.MergeCoplanarFaces(0.0001);
            if (BrepSolidOrientation.Inward == body.SolidOrientation)
                body.Flip();

            result = body;
            return true;
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_SplineJoints;

        public override Guid ComponentGuid => new Guid("7C2F5B18-E9A4-4D06-B3C1-8F47A0E256D9");
    }
}
