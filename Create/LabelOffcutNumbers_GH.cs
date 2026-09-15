/*
 * MIT License
 * 
 * Copyright (c) 2022 Dominik Reisach
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */


using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace SpruceBeetle.Create
{
    public class LabelOffcutNumbers_GH : GH_Component
    {
        const double Tol = 0.0001;

        public LabelOffcutNumbers_GH()
          : base("Label Offcut Numbers", "LabelN", "Cut the stock number into the largest vertical face of each Offcut", "Spruce Beetle", "     Create")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed or aligned Offcuts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Size", "S", "Letter height (model units). Clamped to fit the face.", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Depth", "Dep", "Cut depth (model units). Clamped so it cannot punch through.", GH_ParamAccess.item, 0.125);

            pManager[1].Optional = true;
            pManager[2].Optional = true;

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts with the stock number cut into the largest vertical face", GH_ParamAccess.list);
            pManager.AddBrepParameter("Cutters", "C", "Letter solids used to cut. Preview these to see the numbers.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Skipped", "Sk", "Faces that were skipped (no vertical face, text failed, or boolean failed)", GH_ParamAccess.list);

            pManager.HideParameter(2);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<SpruceBeetle.Offcut> offcuts = new List<SpruceBeetle.Offcut>();
            double size = 1.0;
            double depth = 0.125;

            if (!DA.GetDataList(0, offcuts)) return;
            DA.GetData(1, ref size);
            DA.GetData(2, ref depth);

            if (size <= 0)
                size = 1.0;
            if (depth <= 0)
                depth = 0.125;

            var output = new List<Offcut_GH>();
            var cutters = new List<Brep>();
            var skipped = new List<Plane>();

            if (offcuts == null || offcuts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The Offcut list is empty!");
                DA.SetDataList(0, output);
                DA.SetDataList(1, cutters);
                DA.SetDataList(2, skipped);
                return;
            }

            int cutCount = 0;
            int failCount = 0;

            for (int i = 0; i < offcuts.Count; i++)
            {
                Offcut source = offcuts[i];
                if (source == null)
                    continue;

                Offcut copy = source.Duplicate();
                Brep solid = source.OffcutGeometry == null ? null : source.OffcutGeometry.DuplicateBrep();
                if (solid != null)
                    copy.OffcutGeometry = solid;

                if (solid == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Offcut has no Brep (not packed or aligned).");
                    output.Add(new Offcut_GH(copy));
                    continue;
                }

                if (!TryGetLargestVerticalFace(solid, out Plane facePlane, out double faceWidth, out double faceHeight, out double thickness))
                {
                    skipped.Add(source.AveragePlane.IsValid ? source.AveragePlane : Plane.WorldXY);
                    output.Add(new Offcut_GH(copy));
                    failCount++;
                    continue;
                }

                double cutDepth = Math.Min(depth, thickness * 0.4);
                if (cutDepth < Tol)
                {
                    skipped.Add(facePlane);
                    output.Add(new Offcut_GH(copy));
                    failCount++;
                    continue;
                }

                if (!TryMakeLetterCutter(FormatIndex(source.Index), facePlane, faceWidth, faceHeight, size, cutDepth, out List<Brep> glyphCutters))
                {
                    skipped.Add(facePlane);
                    output.Add(new Offcut_GH(copy));
                    failCount++;
                    continue;
                }

                if (!TryCut(solid, glyphCutters, out Brep cutSolid))
                {
                    skipped.Add(facePlane);
                    copy.OffcutGeometry = solid;
                    output.Add(new Offcut_GH(copy));
                    failCount++;
                    continue;
                }

                copy.OffcutGeometry = cutSolid;
                try
                {
                    copy.FabVol = cutSolid.GetVolume(Tol, Tol);
                }
                catch
                {
                }

                cutters.AddRange(glyphCutters);
                output.Add(new Offcut_GH(copy));
                cutCount++;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                cutCount + " number(s) cut, " + skipped.Count + " skipped, " + failCount + " fail(s).");

            DA.SetDataList(0, output);
            DA.SetDataList(1, cutters);
            DA.SetDataList(2, skipped);
        }


        static bool TryGetLargestVerticalFace(Brep brep, out Plane plane, out double faceWidth, out double faceHeight, out double thickness)
        {
            plane = Plane.Unset;
            faceWidth = 0;
            faceHeight = 0;
            thickness = 0;

            int best = -1;
            double bestArea = -1;
            Point3d bestCenter = Point3d.Origin;
            Vector3d bestNormal = Vector3d.XAxis;

            for (int i = 0; i < brep.Faces.Count; i++)
            {
                BrepFace face = brep.Faces[i];
                AreaMassProperties amp = AreaMassProperties.Compute(face);
                if (amp == null || amp.Area <= Tol)
                    continue;

                Interval du = face.Domain(0);
                Interval dv = face.Domain(1);
                Vector3d n = face.NormalAt(du.Mid, dv.Mid);
                if (!n.Unitize())
                    continue;

                if (Math.Abs(n * Vector3d.ZAxis) > 0.5)
                    continue;

                Point3d center = amp.Centroid;
                bool better = amp.Area > bestArea + 1e-9;
                if (!better && Math.Abs(amp.Area - bestArea) <= 1e-9)
                {
                    if (center.X > bestCenter.X + 1e-9)
                        better = true;
                    else if (Math.Abs(center.X - bestCenter.X) <= 1e-9 && center.Y > bestCenter.Y + 1e-9)
                        better = true;
                }

                if (!better)
                    continue;

                best = i;
                bestArea = amp.Area;
                bestCenter = center;
                bestNormal = n;
            }

            if (best < 0)
                return false;

            Vector3d zAxis = bestNormal;
            Vector3d yAxis = Vector3d.ZAxis - zAxis * (Vector3d.ZAxis * zAxis);
            if (!yAxis.Unitize())
                return false;

            Vector3d xAxis = Vector3d.CrossProduct(yAxis, zAxis);
            if (!xAxis.Unitize())
                return false;
            yAxis = Vector3d.CrossProduct(zAxis, xAxis);
            yAxis.Unitize();

            plane = new Plane(bestCenter, xAxis, yAxis);

            Brep faceBrep = brep.Faces[best].DuplicateFace(false);
            if (faceBrep == null)
                return false;

            GetPlaneBounds(faceBrep, plane, out faceWidth, out faceHeight);
            thickness = ThicknessAlong(brep, zAxis);
            return faceWidth > Tol && faceHeight > Tol && thickness > Tol;
        }


        static void GetPlaneBounds(Brep brep, Plane plane, out double width, out double height)
        {
            width = 0;
            height = 0;
            BoundingBox box = brep.GetBoundingBox(true);
            if (!box.IsValid)
                return;

            double minU = double.MaxValue;
            double maxU = double.MinValue;
            double minV = double.MaxValue;
            double maxV = double.MinValue;
            Point3d[] corners = box.GetCorners();
            for (int i = 0; i < corners.Length; i++)
            {
                plane.ClosestParameter(corners[i], out double u, out double v);
                if (u < minU) minU = u;
                if (u > maxU) maxU = u;
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;
            }

            width = maxU - minU;
            height = maxV - minV;
        }


        static double ThicknessAlong(Brep brep, Vector3d axis)
        {
            BoundingBox box = brep.GetBoundingBox(true);
            if (!box.IsValid)
                return 0;

            double min = double.MaxValue;
            double max = double.MinValue;
            Point3d[] corners = box.GetCorners();
            for (int i = 0; i < corners.Length; i++)
            {
                double t = axis * (corners[i] - Point3d.Origin);
                if (t < min) min = t;
                if (t > max) max = t;
            }

            return max - min;
        }


        static bool TryMakeLetterCutter(string text, Plane facePlane, double faceWidth, double faceHeight, double size, double depth, out List<Brep> cutterParts)
        {
            cutterParts = null;
            double maxHeight = 0.8 * Math.Min(faceWidth, faceHeight);
            double height = Math.Min(size, maxHeight);
            if (height < Tol * 10)
                return false;

            Curve[] outlines = Curve.CreateTextOutlines(text, "Arial", height, 0, true, facePlane, 1.0, Tol);
            if (outlines == null || outlines.Length == 0)
                return false;

            BoundingBox textBox = BoundingBox.Empty;
            for (int i = 0; i < outlines.Length; i++)
            {
                if (outlines[i] == null)
                    continue;
                textBox.Union(outlines[i].GetBoundingBox(true));
            }

            if (!textBox.IsValid)
                return false;

            Transform toCenter = Transform.Translation(facePlane.Origin - textBox.Center);
            for (int i = 0; i < outlines.Length; i++)
            {
                if (outlines[i] != null)
                    outlines[i].Transform(toCenter);
            }

            GetPlaneBoundsFromCurves(outlines, facePlane, out double textW, out double textH);
            double fit = 1.0;
            if (textW > Tol)
                fit = Math.Min(fit, 0.8 * faceWidth / textW);
            if (textH > Tol)
                fit = Math.Min(fit, 0.8 * faceHeight / textH);
            if (fit < 1.0 - 1e-9)
            {
                Transform scale = Transform.Scale(facePlane.Origin, fit);
                for (int i = 0; i < outlines.Length; i++)
                {
                    if (outlines[i] != null)
                        outlines[i].Transform(scale);
                }
            }

            Brep[] planar = Brep.CreatePlanarBreps(outlines, Tol);
            if (planar == null || planar.Length == 0)
                return false;

            double overlap = Math.Min(0.01, depth * 0.25);
            if (overlap < Tol)
                overlap = Tol;

            Vector3d inward = -facePlane.ZAxis * (depth + overlap);
            Curve path = new Line(facePlane.Origin + facePlane.ZAxis * overlap, facePlane.Origin + facePlane.ZAxis * overlap + inward).ToNurbsCurve();

            var parts = new List<Brep>();
            Transform start = Transform.Translation(facePlane.ZAxis * overlap);
            for (int i = 0; i < planar.Length; i++)
            {
                if (planar[i] == null || planar[i].Faces.Count == 0)
                    continue;

                Brep slab = planar[i].DuplicateBrep();
                slab.Transform(start);
                Brep extruded = slab.Faces[0].CreateExtrusion(path, true);
                if (extruded == null)
                    continue;

                extruded.Faces.SplitKinkyFaces(Tol);
                if (BrepSolidOrientation.Inward == extruded.SolidOrientation)
                    extruded.Flip();
                parts.Add(extruded);
            }

            if (parts.Count == 0)
                return false;

            // Multi-digit numbers (11, 48, …) are disjoint glyphs. Join/Union of
            // non-touching solids fails or keeps only the first digit — keep every part.
            cutterParts = parts;
            return true;
        }


        static void GetPlaneBoundsFromCurves(Curve[] curves, Plane plane, out double width, out double height)
        {
            width = 0;
            height = 0;
            double minU = double.MaxValue;
            double maxU = double.MinValue;
            double minV = double.MaxValue;
            double maxV = double.MinValue;
            bool any = false;

            for (int i = 0; i < curves.Length; i++)
            {
                if (curves[i] == null)
                    continue;
                BoundingBox box = curves[i].GetBoundingBox(true);
                if (!box.IsValid)
                    continue;

                Point3d[] corners = box.GetCorners();
                for (int j = 0; j < corners.Length; j++)
                {
                    plane.ClosestParameter(corners[j], out double u, out double v);
                    if (u < minU) minU = u;
                    if (u > maxU) maxU = u;
                    if (v < minV) minV = v;
                    if (v > maxV) maxV = v;
                    any = true;
                }
            }

            if (!any)
                return;

            width = maxU - minU;
            height = maxV - minV;
        }


        static bool TryCut(Brep solid, List<Brep> cutterParts, out Brep result)
        {
            result = solid;
            if (solid == null || cutterParts == null || cutterParts.Count == 0)
                return false;

            var tools = new List<Brep>();
            for (int i = 0; i < cutterParts.Count; i++)
            {
                if (cutterParts[i] != null)
                    tools.Add(cutterParts[i]);
            }

            if (tools.Count == 0)
                return false;

            Brep[] cut = Brep.CreateBooleanDifference(new[] { solid }, tools.ToArray(), Tol);
            if (cut != null && cut.Length > 0 && cut[0] != null)
            {
                result = FinishCut(cut[0]);
                return true;
            }

            // Subtract one glyph at a time if the batch boolean fails.
            Brep current = solid;
            int cuts = 0;
            for (int i = 0; i < tools.Count; i++)
            {
                Brep[] one = Brep.CreateBooleanDifference(new[] { current }, new[] { tools[i] }, Tol);
                if (one == null || one.Length == 0 || one[0] == null)
                    continue;

                current = FinishCut(one[0]);
                cuts++;
            }

            if (cuts == 0)
                return false;

            result = current;
            return cuts == tools.Count;
        }


        static Brep FinishCut(Brep body)
        {
            body.Faces.SplitKinkyFaces(Tol);
            if (BrepSolidOrientation.Inward == body.SolidOrientation)
                body.Flip();
            return body;
        }


        static string FormatIndex(double index)
        {
            double rounded = Math.Round(index);
            if (Math.Abs(index - rounded) < 1e-9)
                return ((long)rounded).ToString();

            return index.ToString();
        }


        public override GH_Exposure Exposure => GH_Exposure.secondary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_GetBrep;

        public override Guid ComponentGuid => new Guid("D4B8A160-2E9C-4F71-8A3D-1C6E90F5B247");
    }
}
