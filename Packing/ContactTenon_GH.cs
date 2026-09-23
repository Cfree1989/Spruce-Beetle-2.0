using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class ContactTenon_GH : GH_Component
    {
        IGH_Param typeParameter = null;

        public ContactTenon_GH()
          : base("Contact Tenon", "ContactTenon",
              "Cut matching tenons on both packed Offcuts at each selected face contact, centered on the shared overlap rectangle",
              "Spruce Beetle", "   Packing")
        {
            ApplyDisplayNames();
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed Offcuts (same list as Packed Contacts)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Contacts", "C", "Selected contacts from Select Contacts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tool Diameter", "D", "CNC bit diameter. Sets the smallest tenon and the smallest corner the bit can cut", GH_ParamAccess.item, 0.25);
            pManager.AddNumberParameter("Joint X", "JX", "Tenon size along the overlap long side (raised to D if smaller)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Joint Y", "JY", "Tenon size along the overlap short side (raised to D if smaller)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Depth", "Dep", "Total tenon depth, centered on the contact plane (clamped to thinner member / 3)", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Tool Radius", "R", "Corner fillet radius (raised to D / 2 if smaller, no warning)", GH_ParamAccess.item, 0.125);
            pManager.AddTextParameter("Joint Type", "JT", "tenon, cross tenon, or custom tenon", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Tenon Count", "TC", "Number of tenons along the overlap long side", GH_ParamAccess.item, 1);
            pManager.AddCurveParameter("Custom Shape", "CS", "Closed planar curve for a custom tenon", GH_ParamAccess.item);
            pManager[9].Optional = true;
            typeParameter = pManager[7];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts after tenon cuts", GH_ParamAccess.list);
            pManager.AddBrepParameter("Joints", "J", "Tenon solids (matching pockets on both members)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Joint Volume", "JV", "Volume of each tenon solid", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Skipped", "Sk", "Planes of contacts that were not cut. Preview only; baking this component skips planes", GH_ParamAccess.list);
            pManager.AddGenericParameter("Skipped Contacts", "SkC", "The same skipped contacts, for a second Contact Tenon with a smaller JX / JY", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        public override bool Read(GH_IReader reader)
        {
            bool ok = base.Read(reader);
            ApplyDisplayNames();
            return ok;
        }


        public override void AddedToDocument(GH_Document document)
        {
            ApplyDisplayNames();
            base.AddedToDocument(document);
        }


        void ApplyDisplayNames()
        {
            Name = "Contact Tenon";
            NickName = "ContactTenon";
        }


        protected override void BeforeSolveInstance()
        {
            if (typeParameter == null)
                return;

            GH_ValueList list = null;
            foreach (var source in typeParameter.Sources)
            {
                if (source is GH_ValueList vl)
                {
                    list = vl;
                    break;
                }
            }

            if (list == null)
            {
                if (Instances.ActiveCanvas?.Document == null)
                    return;

                list = new GH_ValueList();
                list.CreateAttributes();
                list.Attributes.Pivot = new System.Drawing.PointF(Attributes.Pivot.X - 200, Attributes.Pivot.Y);
                Instances.ActiveCanvas.Document.AddObject(list, false);
                typeParameter.AddSource(list);
            }

            SyncTypeList(list);
            typeParameter.CollectData();
        }


        private static void SyncTypeList(GH_ValueList list)
        {
            List<string> wanted = Joint.TenonTypes();
            if (list.ListItems.Count == wanted.Count)
            {
                bool same = true;
                for (int i = 0; i < wanted.Count; i++)
                {
                    if (!string.Equals(list.ListItems[i].Name, wanted[i], StringComparison.Ordinal))
                    {
                        same = false;
                        break;
                    }
                }
                if (same)
                    return;
            }

            list.ListItems.Clear();
            foreach (string type in wanted)
                list.ListItems.Add(new GH_ValueListItem(type, $"\"{type}\""));
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var packed = new List<Offcut>();
            var objs = new List<object>();
            double diameter = 0.25;
            double jointX = 1.0;
            double jointY = 1.0;
            double depthRequest = 0.5;
            double toolRadius = 0.125;
            string jointKey = "tenon";
            int tenonCount = 1;
            Curve jointShape = null;

            if (!DA.GetDataList(0, packed))
                return;
            if (!DA.GetDataList(1, objs))
                return;
            DA.GetData(2, ref diameter);
            DA.GetData(3, ref jointX);
            DA.GetData(4, ref jointY);
            DA.GetData(5, ref depthRequest);
            DA.GetData(6, ref toolRadius);
            DA.GetData(7, ref jointKey);
            DA.GetData(8, ref tenonCount);
            DA.GetData(9, ref jointShape);

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

            if (jointX < diameter || jointY < diameter)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"JX / JY raised to the tool diameter ({diameter}).");
                jointX = Math.Max(jointX, diameter);
                jointY = Math.Max(jointY, diameter);
            }

            double minRadius = diameter * 0.5;
            if (toolRadius < minRadius)
                toolRadius = minRadius;

            if (tenonCount < 1)
                tenonCount = 1;

            if (jointShape != null)
            {
                if (!jointShape.IsClosed)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The custom curve is not closed!");
                else if (!jointShape.IsPlanar())
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The custom curve is not planar!");
            }

            int jointType = ParseJointType(jointKey);
            if (jointType == 2 && (jointShape == null || !jointShape.IsClosed || !jointShape.IsPlanar()))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "custom tenon needs a closed planar curve on CS; those contacts will be skipped.");

            var boxes = new BoundingBox[packed.Count];
            var geometry = new Brep[packed.Count];
            for (int i = 0; i < packed.Count; i++)
            {
                boxes[i] = PackedNeighbors.WorldBox(packed[i]);
                geometry[i] = packed[i].OffcutGeometry?.DuplicateBrep();
            }

            var cutters = new List<Brep>();
            var volumes = new List<double>();
            var skipped = new List<Plane>();
            var skippedContacts = new List<PackedContact_GH>();
            int cutCount = 0;
            int failCount = 0;

            void Skip(PackedContact contact, Plane plane)
            {
                skipped.Add(plane);
                skippedContacts.Add(new PackedContact_GH(contact));
            }

            for (int i = 0; i < objs.Count; i++)
            {
                PackedContact contact = PackedContact_GH.Parse(objs[i]);
                if (contact == null)
                    continue;
                if (contact.IndexA < 0 || contact.IndexB < 0 || contact.IndexA >= packed.Count || contact.IndexB >= packed.Count)
                {
                    Skip(contact, contact.Plane);
                    continue;
                }

                if (!PackedNeighbors.OrientOnOverlap(contact, jointX, jointY, tenonCount, diameter, out Plane placed, out double longSide, out _, out bool canCut) || !canCut)
                {
                    Skip(contact, contact.Plane);
                    continue;
                }

                double depth = PocketDepth(boxes[contact.IndexA], boxes[contact.IndexB], contact.Axis, depthRequest);
                if (depth <= 0)
                {
                    Skip(contact, placed);
                    continue;
                }

                if (jointType == 2 && (jointShape == null || !jointShape.IsClosed || !jointShape.IsPlanar()))
                {
                    Skip(contact, placed);
                    continue;
                }

                if (!TryCreateTenons(placed, jointX, jointY, depth, toolRadius, tenonCount, longSide, jointType, jointShape, out Brep[] joints))
                {
                    Skip(contact, placed);
                    continue;
                }

                bool okA = true;
                bool okB = true;
                for (int j = 0; j < joints.Length; j++)
                {
                    cutters.Add(joints[j]);
                    volumes.Add(SafeVolume(joints[j]));
                    if (!TryCut(geometry[contact.IndexA], joints[j], out geometry[contact.IndexA]))
                        okA = false;
                    if (!TryCut(geometry[contact.IndexB], joints[j], out geometry[contact.IndexB]))
                        okB = false;
                }

                if (okA && okB)
                    cutCount++;
                else
                {
                    failCount++;
                    Skip(contact, placed);
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
                $"{cutCount} contact(s) cut, {skipped.Count} skipped, {failCount} boolean fail(s).");

            DA.SetDataList(0, output);
            DA.SetDataList(1, cutters);
            DA.SetDataList(2, volumes);
            DA.SetDataList(3, skipped);
            DA.SetDataList(4, skippedContacts);
        }


        private int ParseJointType(string jointKey)
        {
            Dictionary<string, int> jointDict = Joint.GetJointType();
            string key = (jointKey ?? "tenon").Trim();
            if (jointDict.TryGetValue(key, out int type))
                return type;

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Unknown joint type '{key}'; using tenon.");
            return 0;
        }


        private static double PocketDepth(BoundingBox a, BoundingBox b, ContactAxis axis, double requested)
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

            if (requested <= 0)
                return 0;

            double cap = Math.Min(thickA, thickB) / 3.0;
            return Math.Min(requested, cap);
        }


        private static bool TryCreateTenons(Plane plane, double jointX, double jointY, double depth, double toolRadius, int tenonCount, double longSide, int jointType, Curve jointShape, out Brep[] joints)
        {
            joints = null;
            switch (jointType)
            {
                case 1:
                    return CrossJoint(plane, jointX, jointY, depth, toolRadius, tenonCount, longSide, out joints);
                case 2:
                    return CreateCustomTenon(jointShape, plane, jointX, jointY, depth, toolRadius, tenonCount, longSide, out joints);
                default:
                    return RectJoint(plane, jointX, jointY, depth, toolRadius, tenonCount, longSide, out joints);
            }
        }


        private static Point3d[] TenonOrigins(Plane plane, double longSide, int tenonCount)
        {
            if (tenonCount <= 1)
                return new[] { plane.Origin };

            Point3d[] pts = Joint.GetPoints(plane, longSide, tenonCount);
            if (pts == null || pts.Length == 0)
                return new[] { plane.Origin };
            return pts;
        }


        private static double FilletRadius(double toolRadius, double jointX, double jointY)
        {
            double max = Math.Min(jointX, jointY) * 0.5 - 1e-4;
            if (max <= 1e-6)
                return 0;
            if (toolRadius <= 0)
                return 0;
            return Math.Min(toolRadius, max);
        }


        private static bool RectJoint(Plane plane, double jointX, double jointY, double depth, double toolRadius, int tenonCount, double longSide, out Brep[] joints)
        {
            joints = null;
            Plane basePlane = plane;
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * depth * 0.5));
            Point3d[] origins = TenonOrigins(basePlane, longSide, tenonCount);
            joints = new Brep[origins.Length];
            Interval dX = new Interval(-jointX * 0.5, jointX * 0.5);
            Interval dY = new Interval(-jointY * 0.5, jointY * 0.5);
            double fillet = FilletRadius(toolRadius, jointX, jointY);

            for (int i = 0; i < origins.Length; i++)
            {
                basePlane.Origin = origins[i];
                var rect = new Rectangle3d(basePlane, dX, dY);
                if (!TryExtrude(rect.ToNurbsCurve(), depth, fillet, out Brep joint))
                    return false;
                joints[i] = joint;
            }

            return true;
        }


        private static bool CrossJoint(Plane plane, double jointX, double jointY, double depth, double toolRadius, int tenonCount, double longSide, out Brep[] joints)
        {
            joints = null;
            Plane basePlane = new Plane(plane);
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * depth * 0.5));
            Point3d[] origins = TenonOrigins(basePlane, longSide, tenonCount);
            joints = new Brep[origins.Length];
            Interval dX = new Interval(-jointX * 0.5, jointX * 0.5);
            Interval dY = new Interval(-jointY * 0.5, jointY * 0.5);
            Interval dXArm = new Interval(-jointY * 0.5, jointY * 0.5);
            Interval dYArm = new Interval(-jointX / 3.0, jointX / 3.0);
            double fillet = FilletRadius(toolRadius, jointX, jointY);

            for (int i = 0; i < origins.Length; i++)
            {
                basePlane.Origin = origins[i];
                var firstRect = new Rectangle3d(basePlane, dX, dY);
                var secondRect = new Rectangle3d(basePlane, dXArm, dYArm);
                Curve outline = firstRect.ToNurbsCurve();
                Curve[] union = Curve.CreateBooleanUnion(new List<Curve> { firstRect.ToNurbsCurve(), secondRect.ToNurbsCurve() }, 0.00001);
                if (union != null && union.Length > 0 && union[0] != null)
                    outline = union[0];

                if (!TryExtrude(outline, depth, fillet, out Brep joint))
                    return false;
                joints[i] = joint;
            }

            return true;
        }


        private static bool CreateCustomTenon(Curve shape, Plane plane, double jointX, double jointY, double depth, double toolRadius, int tenonCount, double longSide, out Brep[] joints)
        {
            joints = null;
            if (shape == null)
                return false;

            Curve template = shape.DuplicateCurve();
            var curveAMP = AreaMassProperties.Compute(template);
            if (curveAMP == null)
                return false;

            Point3d curveCenter = curveAMP.Centroid;
            Plane curvePlane = new Plane(curveCenter, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));
            Box box = new Box(template.GetBoundingBox(true));
            if (box.X.Length < 1e-9 || box.Y.Length < 1e-9)
                return false;

            template.Transform(Transform.Scale(curvePlane, jointX / box.X.Length, jointY / box.Y.Length, 1));

            Plane basePlane = new Plane(plane);
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * depth * 0.5));
            Point3d[] origins = TenonOrigins(basePlane, longSide, tenonCount);
            joints = new Brep[origins.Length];
            double fillet = FilletRadius(toolRadius, jointX, jointY);

            for (int i = 0; i < origins.Length; i++)
            {
                basePlane.Origin = origins[i];
                Curve placed = template.DuplicateCurve();
                placed.Transform(Transform.PlaneToPlane(curvePlane, basePlane));
                if (!placed.IsClosed)
                    placed.MakeClosed(0.0001);
                if (!TryExtrude(placed, depth, fillet, out Brep joint))
                    return false;
                joints[i] = joint;
            }

            return true;
        }


        private static bool TryExtrude(Curve outline, double depth, double fillet, out Brep pocket)
        {
            pocket = null;
            if (outline == null)
                return false;

            Curve profile = outline;
            if (fillet > 1e-6)
            {
                Curve filleted = Curve.CreateFilletCornersCurve(outline, fillet, 0.0001, 0.0001);
                if (filleted != null)
                    profile = filleted;
            }

            Brep extrude = Extrusion.Create(profile, depth, true)?.ToBrep();
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


        private static double SafeVolume(Brep brep)
        {
            if (brep == null)
                return 0;
            try
            {
                return brep.GetVolume(0.0001, 0.0001);
            }
            catch
            {
                return 0;
            }
        }


        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, doc?.CreateDefaultAttributes(), obj_ids);
        }


        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (doc == null)
                return;

            foreach (IGH_Param param in Params.Output)
            {
                if (param is Param_Plane)
                    continue;
                if (param is IGH_BakeAwareObject baker)
                    baker.BakeGeometry(doc, att, obj_ids);
            }
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_TenonJoints;

        public override Guid ComponentGuid => new Guid("7C2F5B18-E9A4-4D06-B3C1-8F47A0E256D9");
    }
}
