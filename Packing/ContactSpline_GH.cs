using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class ContactSpline_GH : GH_Component
    {
        public ContactSpline_GH()
          : base("Contact Spline", "ContactSpline",
              "Cut an edge-open spline slot on both packed Offcuts so a loose key can be driven in after the pieces are stacked",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed Offcuts (same list as Packed Contacts)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Contacts", "C", "Selected contacts from Select Contacts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tool Diameter", "D", "CNC bit diameter. Floors JY and the corner fillet", GH_ParamAccess.item, 0.25);
            pManager.AddNumberParameter("Joint X", "JX", "Key length along the slot run (closed end keeps D of meat)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Joint Y", "JY", "Slot width across the run (raised to D if smaller)", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Depth", "Dep", "Total slot depth, centered on the contact plane (clamped to thinner member / 3)", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Tool Radius", "R", "Corner fillet radius (raised to D / 2 if smaller, no warning)", GH_ParamAccess.item, 0.125);
            pManager.AddIntegerParameter("Tenon Count", "TC", "Number of parallel channels across the overlap", GH_ParamAccess.item, 1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts after spline cuts", GH_ParamAccess.list);
            pManager.AddBrepParameter("Joints", "J", "Key solids (one per channel)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Joint Volume", "JV", "Volume of each key solid", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Skipped", "Sk", "Planes of contacts that were not cut (preview these to see skips in the viewport)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Skipped Contacts", "SkC", "The same skipped contacts, for a second Contact Spline with a smaller JX / JY", GH_ParamAccess.list);
            pManager.AddLineParameter("Direction", "Dir", "Drive-in line per key, from the mouth toward the closed stop", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
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
            int channelCount = 1;

            if (!DA.GetDataList(0, packed))
                return;
            if (!DA.GetDataList(1, objs))
                return;
            DA.GetData(2, ref diameter);
            DA.GetData(3, ref jointX);
            DA.GetData(4, ref jointY);
            DA.GetData(5, ref depthRequest);
            DA.GetData(6, ref toolRadius);
            DA.GetData(7, ref channelCount);

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

            if (jointY < diameter)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"JY raised to the tool diameter ({diameter}).");
                jointY = diameter;
            }

            double minRadius = diameter * 0.5;
            if (toolRadius < minRadius)
                toolRadius = minRadius;

            if (channelCount < 1)
                channelCount = 1;

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

                if (!PackedNeighbors.TrySplineMouth(contact, boxes, jointX, jointY, channelCount, diameter, out SplineMouth mouth) || mouth == null)
                {
                    Skip(contact, contact.Plane);
                    continue;
                }

                double depth = PocketDepth(boxes[contact.IndexA], boxes[contact.IndexB], contact.Axis, depthRequest);
                if (depth <= 0)
                {
                    Skip(contact, mouth.Frame);
                    continue;
                }

                if (!TryCreateSplines(mouth, jointX, jointY, depth, toolRadius, channelCount, diameter, out Brep[] cutters, out Brep[] keySolids, out Line[] dirs))
                {
                    Skip(contact, mouth.Frame);
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
                    cutCount++;
                else
                {
                    failCount++;
                    Skip(contact, mouth.Frame);
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
            DA.SetDataList(1, keys);
            DA.SetDataList(2, volumes);
            DA.SetDataList(3, skipped);
            DA.SetDataList(4, skippedContacts);
            DA.SetDataList(5, directions);
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


        private static bool TryCreateSplines(SplineMouth mouth, double jointX, double jointY, double depth, double toolRadius, int channelCount, double diameter, out Brep[] cutters, out Brep[] keys, out Line[] dirs)
        {
            cutters = null;
            keys = null;
            dirs = null;

            Plane frame = mouth.Frame;
            double run = mouth.RunLength;
            double across = mouth.AcrossLength;
            const double hair = 0.01;
            double meat = Math.Max(diameter, 0);
            double stop = run * 0.5 - meat;
            double mouthX = -run * 0.5;
            double keyStart = stop - jointX;

            Point3d[] origins = ChannelOrigins(frame, across, channelCount);
            cutters = new Brep[origins.Length];
            keys = new Brep[origins.Length];
            dirs = new Line[origins.Length];
            double fillet = FilletRadius(toolRadius, jointX, jointY);

            var cutterX = new Interval(mouthX - hair, stop);
            var keyX = new Interval(keyStart, stop);
            var dY = new Interval(-jointY * 0.5, jointY * 0.5);

            for (int i = 0; i < origins.Length; i++)
            {
                Plane basePlane = frame;
                basePlane.Origin = origins[i];
                basePlane.Transform(Transform.Translation(-basePlane.ZAxis * depth * 0.5));

                var keyRect = new Rectangle3d(basePlane, keyX, dY);
                Curve cutterProfile = SlotProfile(basePlane, cutterX, dY, fillet);
                if (!TryExtrude(cutterProfile, depth, out Brep cutter))
                    return false;
                if (!TryExtrude(keyRect.ToNurbsCurve(), depth, out Brep key))
                    return false;

                Point3d mouthPt = basePlane.PointAt(mouthX, 0, depth * 0.5);
                Point3d stopPt = basePlane.PointAt(stop, 0, depth * 0.5);
                cutters[i] = cutter;
                keys[i] = key;
                dirs[i] = new Line(mouthPt, stopPt);
            }

            return true;
        }


        private static Point3d[] ChannelOrigins(Plane plane, double acrossLength, int channelCount)
        {
            if (channelCount <= 1)
                return new[] { plane.Origin };

            Plane across = plane;
            across.Rotate(Math.PI * 0.5, plane.ZAxis, plane.Origin);
            Point3d[] pts = Joint.GetPoints(across, acrossLength, channelCount);
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


        /// <summary>
        /// Rectangle in the slot plane. Fillet only the closed-stop corners so the mouth
        /// meets the member edge at 90 degrees.
        /// </summary>
        private static Curve SlotProfile(Plane plane, Interval x, Interval y, double fillet)
        {
            var rect = new Rectangle3d(plane, x, y);
            Curve sharp = rect.ToNurbsCurve();
            if (fillet <= 1e-6 || sharp == null)
                return sharp;

            double run = x.Length;
            double width = y.Length;
            double r = Math.Min(fillet, Math.Min(run, width) * 0.5 - 1e-4);
            if (r <= 1e-6)
                return sharp;

            Vector3d xDir = plane.XAxis;
            Vector3d yDir = plane.YAxis;
            Point3d a = plane.PointAt(x.Min, y.Min, 0);
            Point3d b = plane.PointAt(x.Max, y.Min, 0);
            Point3d c = plane.PointAt(x.Max, y.Max, 0);
            Point3d d = plane.PointAt(x.Min, y.Max, 0);

            Point3d b1 = b - xDir * r;
            Point3d b2 = b + yDir * r;
            Point3d centerB = b - xDir * r + yDir * r;
            Point3d midB = centerB + (xDir - yDir) * (r / Math.Sqrt(2.0));

            Point3d c1 = c - yDir * r;
            Point3d c2 = c - xDir * r;
            Point3d centerC = c - xDir * r - yDir * r;
            Point3d midC = centerC + (xDir + yDir) * (r / Math.Sqrt(2.0));

            var parts = new List<Curve>
            {
                new Line(a, b1).ToNurbsCurve(),
                new Arc(b1, midB, b2).ToNurbsCurve(),
                new Line(b2, c1).ToNurbsCurve(),
                new Arc(c1, midC, c2).ToNurbsCurve(),
                new Line(c2, d).ToNurbsCurve(),
                new Line(d, a).ToNurbsCurve()
            };

            Curve[] joined = Curve.JoinCurves(parts, 0.0001);
            if (joined == null || joined.Length == 0 || joined[0] == null)
                return sharp;

            Curve profile = joined[0];
            if (!profile.IsClosed)
                profile.MakeClosed(0.0001);
            return profile;
        }


        private static bool TryExtrude(Curve outline, double depth, out Brep pocket)
        {
            pocket = null;
            if (outline == null)
                return false;

            Curve profile = outline;

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


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_SplineJoints;

        public override Guid ComponentGuid => new Guid("A8D31C47-6E2B-4F90-9C14-5B7A2E8D0146");
    }
}
