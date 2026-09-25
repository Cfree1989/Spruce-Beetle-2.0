using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class OutsideKey_GH : GH_Component
    {
        IGH_Param typeParameter = null;
        IGH_Param clearanceParameter = null;

        public OutsideKey_GH()
          : base("Outside Key", "OutsideKey",
              "Cut a face key on the packed column skin, centered on seams where both Offcuts share that outer face",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed Offcuts (same list as Packed Contacts)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Contacts", "C", "Selected contacts from Packed Contacts or Select Contacts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tool Diameter", "D", "CNC bit diameter. Floors JX, JY, and the corner fillet", GH_ParamAccess.item, 0.25);
            pManager.AddNumberParameter("Joint X", "JX", "Key length along the seam (raised to D if smaller)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Joint Y", "JY", "Key width across the seam, split into both pieces (raised to D if smaller)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Depth", "Dep", "Depth into the outer face (clamped to thinner member / 3)", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Tool Radius", "R", "Corner fillet on a rectangular key (raised to D / 2 if smaller, no warning)", GH_ParamAccess.item, 0.125);
            pManager.AddTextParameter("Joint Type", "JT", "rectangular or custom key", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Tenon Count", "TC", "Number of keys along the exposed seam", GH_ParamAccess.item, 1);
            pManager.AddCurveParameter("Custom Shape", "CS", "Closed planar curve for a custom key", GH_ParamAccess.item);
            pManager[9].Optional = true;
            pManager.AddNumberParameter("Clearance", "Cl", "Gap on each side between the key and the pocket, plus extra depth into the face. Auto slider runs from 0.001 to 0.01", GH_ParamAccess.item, ClearanceSlider.Default);
            typeParameter = pManager[7];
            clearanceParameter = pManager[10];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts after face-key cuts", GH_ParamAccess.list);
            pManager.AddBrepParameter("Joints", "J", "Key solids (flush with the outer face)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Joint Volume", "JV", "Volume of each key solid", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Skipped", "Sk", "Planes of contacts that were not cut. Preview only; baking this component skips planes", GH_ParamAccess.list);
            pManager.AddGenericParameter("Skipped Contacts", "SkC", "The same skipped contacts, for a second Outside Key with a smaller JX / JY", GH_ParamAccess.list);
            pManager.AddLineParameter("Direction", "Dir", "Drive-in line per key, from outside the face inward", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void BeforeSolveInstance()
        {
            ClearanceSlider.Ensure(clearanceParameter, this);
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
            List<string> wanted = Joint.OutsideKeyTypes();
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
            string jointKey = "rectangular";
            int keyCount = 1;
            Curve jointShape = null;
            double clearance = ClearanceSlider.Default;

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
            DA.GetData(8, ref keyCount);
            DA.GetData(9, ref jointShape);
            DA.GetData(10, ref clearance);
            clearance = ClearanceSlider.Read(this, clearance);

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

            if (keyCount < 1)
                keyCount = 1;

            if (jointShape != null)
            {
                if (!jointShape.IsClosed)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The custom curve is not closed!");
                else if (!jointShape.IsPlanar())
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The custom curve is not planar!");
            }

            bool custom = IsCustomKey(jointKey);
            if (custom && (jointShape == null || !jointShape.IsClosed || !jointShape.IsPlanar()))
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "custom key needs a closed planar curve on CS; those contacts will be skipped.");

            var boxes = new BoundingBox[packed.Count];
            var geometry = new Brep[packed.Count];
            for (int i = 0; i < packed.Count; i++)
            {
                boxes[i] = PackedNeighbors.WorldBox(packed[i]);
                geometry[i] = packed[i].OffcutGeometry?.DuplicateBrep();
            }

            var keys = new List<Brep>();
            var volumes = new List<double>();
            var skipped = new List<Plane>();
            var skippedContacts = new List<PackedContact_GH>();
            var directions = new List<Line>();
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

                if (!PackedNeighbors.TryOutsideSeats(contact, boxes, out List<OutsideSeat> seats) || seats == null || seats.Count == 0)
                {
                    Skip(contact, contact.Plane);
                    continue;
                }

                if (custom && (jointShape == null || !jointShape.IsClosed || !jointShape.IsPlanar()))
                {
                    Skip(contact, contact.Plane);
                    continue;
                }

                int seatsCut = 0;
                bool anyFail = false;
                for (int s = 0; s < seats.Count; s++)
                {
                    OutsideSeat seat = seats[s];
                    if (!SeatFits(seat, jointX, jointY, keyCount, diameter, depthRequest, clearance, out double depth))
                        continue;

                    if (!TryCreateKeys(seat, jointX, jointY, depth, clearance, toolRadius, keyCount, custom, jointShape, out Brep[] cutters, out Brep[] keySolids, out Line[] dirs))
                    {
                        anyFail = true;
                        continue;
                    }

                    bool okA = true;
                    bool okB = true;
                    for (int j = 0; j < cutters.Length; j++)
                    {
                        keys.Add(keySolids[j]);
                        volumes.Add(SafeVolume(keySolids[j]));
                        directions.Add(dirs[j]);
                        if (!TryCut(geometry[contact.IndexA], cutters[j], out geometry[contact.IndexA]))
                            okA = false;
                        if (!TryCut(geometry[contact.IndexB], cutters[j], out geometry[contact.IndexB]))
                            okB = false;
                    }

                    if (okA && okB)
                        seatsCut++;
                    else
                        anyFail = true;
                }

                if (seatsCut == 0)
                    Skip(contact, contact.Plane);
                else
                    cutCount++;

                if (anyFail)
                    failCount++;
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
            DA.SetDataList(1, keys);
            DA.SetDataList(2, volumes);
            DA.SetDataList(3, skipped);
            DA.SetDataList(4, skippedContacts);
            DA.SetDataList(5, directions);
        }


        private bool IsCustomKey(string jointKey)
        {
            string key = (jointKey ?? "rectangular").Trim();
            if (string.Equals(key, "custom key", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(key, "rectangular", StringComparison.OrdinalIgnoreCase))
                return false;

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Unknown joint type '{key}'; using rectangular.");
            return false;
        }


        private static bool SeatFits(OutsideSeat seat, double jointX, double jointY, int keyCount, double diameter, double depthRequest, double clearance, out double depth)
        {
            depth = 0;
            int count = Math.Max(keyCount, 1);
            double margin = Math.Max(diameter, 0);
            double fitX = jointX + 2.0 * clearance;
            double fitY = jointY + 2.0 * clearance;
            if (fitX * count > seat.SeamLength - 2.0 * margin + 1e-9)
                return false;
            if (fitY * 0.5 > seat.AcrossA + 1e-9 || fitY * 0.5 > seat.AcrossB + 1e-9)
                return false;
            if (depthRequest <= 0)
                return false;

            double cap = Math.Min(seat.InwardA, seat.InwardB) / 3.0;
            double pocket = Math.Min(depthRequest + clearance, cap);
            depth = pocket - clearance;
            return depth > 1e-6;
        }


        private static bool TryCreateKeys(
            OutsideSeat seat,
            double jointX,
            double jointY,
            double depth,
            double clearance,
            double toolRadius,
            int keyCount,
            bool custom,
            Curve jointShape,
            out Brep[] cutters,
            out Brep[] keys,
            out Line[] dirs)
        {
            cutters = null;
            keys = null;
            dirs = null;

            Point3d[] origins = KeyOrigins(seat.Frame, seat.SeamLength, keyCount);
            cutters = new Brep[origins.Length];
            keys = new Brep[origins.Length];
            dirs = new Line[origins.Length];
            const double hair = 0.01;
            double fillet = custom ? 0 : FilletRadius(toolRadius, jointX, jointY);
            double cutDepth = depth + clearance;

            for (int i = 0; i < origins.Length; i++)
            {
                Plane face = seat.Frame;
                face.Origin = origins[i];

                if (!TryProfile(face, jointX, jointY, fillet, custom, jointShape, out Curve profile))
                    return false;

                Curve cutterProfile;
                if (custom)
                {
                    cutterProfile = clearance <= 1e-9
                        ? profile.DuplicateCurve()
                        : Joint.GrowClosed(profile, face, clearance);
                    if (cutterProfile == null)
                        return false;
                }
                else if (!TryProfile(face, jointX + 2.0 * clearance, jointY + 2.0 * clearance, fillet, false, null, out cutterProfile))
                {
                    return false;
                }

                Plane cutterPlane = face;
                cutterPlane.Origin = face.Origin - face.ZAxis * hair;
                cutterProfile.Transform(Transform.PlaneToPlane(face, cutterPlane));

                if (!TryExtrude(cutterProfile, cutDepth + hair, out Brep cutter))
                    return false;
                if (!TryExtrude(profile, depth, out Brep key))
                    return false;

                cutters[i] = cutter;
                keys[i] = key;
                dirs[i] = new Line(face.Origin - face.ZAxis * hair, face.Origin + face.ZAxis * depth);
            }

            return true;
        }


        private static Point3d[] KeyOrigins(Plane plane, double seamLength, int keyCount)
        {
            if (keyCount <= 1)
                return new[] { plane.Origin };

            Point3d[] pts = Joint.GetPoints(plane, seamLength, keyCount);
            if (pts == null || pts.Length == 0)
                return new[] { plane.Origin };
            return pts;
        }


        private static bool TryProfile(Plane plane, double jointX, double jointY, double fillet, bool custom, Curve jointShape, out Curve profile)
        {
            profile = null;
            if (custom)
            {
                if (jointShape == null)
                    return false;

                Curve template = jointShape.DuplicateCurve();
                var curveAMP = AreaMassProperties.Compute(template);
                if (curveAMP == null)
                    return false;

                Point3d curveCenter = curveAMP.Centroid;
                Plane curvePlane = new Plane(curveCenter, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));
                Box box = new Box(template.GetBoundingBox(true));
                if (box.X.Length < 1e-9 || box.Y.Length < 1e-9)
                    return false;

                template.Transform(Transform.Scale(curvePlane, jointX / box.X.Length, jointY / box.Y.Length, 1));
                template.Transform(Transform.PlaneToPlane(curvePlane, plane));
                if (!template.IsClosed)
                    template.MakeClosed(0.0001);
                profile = template;
                return true;
            }

            var dX = new Interval(-jointX * 0.5, jointX * 0.5);
            var dY = new Interval(-jointY * 0.5, jointY * 0.5);
            var rect = new Rectangle3d(plane, dX, dY);
            Curve outline = rect.ToNurbsCurve();
            if (outline == null)
                return false;

            if (fillet > 1e-6)
            {
                Curve filleted = Curve.CreateFilletCornersCurve(outline, fillet, 0.0001, 0.0001);
                if (filleted != null)
                    outline = filleted;
            }

            profile = outline;
            return true;
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


        private static bool TryExtrude(Curve outline, double depth, out Brep pocket)
        {
            pocket = null;
            if (outline == null)
                return false;

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

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_SplineJoints;

        public override Guid ComponentGuid => new Guid("9B4E7D12-5A83-4C6F-B291-0E8F3A47D5C1");
    }
}
