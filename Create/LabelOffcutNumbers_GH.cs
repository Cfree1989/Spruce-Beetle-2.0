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
using SpruceBeetle.Packing;


namespace SpruceBeetle.Create
{
    public class LabelOffcutNumbers_GH : GH_Component
    {
        const double Tol = 0.0001;
        const double ContactTol = 0.01;

        public LabelOffcutNumbers_GH()
          : base("Label Offcut Numbers", "LabelN", "Cut the stock number into an exposed (non-contact) face of each Offcut", "Spruce Beetle", "     Create")
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
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts with the stock number cut into an exposed face", GH_ParamAccess.list);
            pManager.AddBrepParameter("Cutters", "C", "Letter solids used to cut. Preview these to see the numbers.", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Skipped", "Sk", "Faces that were skipped (no usable face, text failed, or boolean failed)", GH_ParamAccess.list);

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

            var boxes = new BoundingBox[offcuts.Count];
            for (int i = 0; i < offcuts.Count; i++)
                boxes[i] = PackedNeighbors.WorldBox(offcuts[i]);

            double minExposed = Math.Max(0.2, size * 0.2);
            int cutCount = 0;
            int exposedCount = 0;
            int buriedCount = 0;
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

                List<LabelCandidate> candidates = CollectLabelCandidates(solid, boxes, i, minExposed);
                if (candidates.Count == 0)
                {
                    skipped.Add(source.AveragePlane.IsValid ? source.AveragePlane : Plane.WorldXY);
                    output.Add(new Offcut_GH(copy));
                    failCount++;
                    continue;
                }

                bool done = false;
                Plane failPlane = candidates[0].Plane;
                for (int c = 0; c < candidates.Count; c++)
                {
                    LabelCandidate cand = candidates[c];
                    double cutDepth = Math.Min(depth, cand.Thickness * 0.4);
                    if (cutDepth < Tol)
                    {
                        failPlane = cand.Plane;
                        continue;
                    }

                    if (!TryMakeLetterCutter(FormatIndex(source.Index), cand.Plane, cand.Width, cand.Height, size, cutDepth, out List<Brep> glyphCutters))
                    {
                        failPlane = cand.Plane;
                        continue;
                    }

                    if (!TryCut(solid, glyphCutters, out Brep cutSolid))
                    {
                        failPlane = cand.Plane;
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
                    if (cand.Rank <= 1)
                        exposedCount++;
                    else
                        buriedCount++;
                    done = true;
                    break;
                }

                if (!done)
                {
                    skipped.Add(failPlane);
                    copy.OffcutGeometry = solid;
                    output.Add(new Offcut_GH(copy));
                    failCount++;
                }
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                cutCount + " number(s) cut (" + exposedCount + " exposed, " + buriedCount + " buried), "
                + skipped.Count + " skipped, " + failCount + " fail(s).");

            DA.SetDataList(0, output);
            DA.SetDataList(1, cutters);
            DA.SetDataList(2, skipped);
        }


        struct LabelCandidate
        {
            public Plane Plane;
            public double Width;
            public double Height;
            public double Thickness;
            public int Rank;
            public double ExposedArea;
            public double FaceArea;
            public Point3d Center;
        }


        struct UVRect
        {
            public double U0;
            public double U1;
            public double V0;
            public double V1;

            public double Width => U1 - U0;
            public double Height => V1 - V0;
            public double Area => Math.Max(0.0, Width) * Math.Max(0.0, Height);
        }


        static List<LabelCandidate> CollectLabelCandidates(Brep brep, BoundingBox[] boxes, int index, double minExposed)
        {
            var list = new List<LabelCandidate>();
            if (brep == null)
                return list;

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

                Point3d center = amp.Centroid;
                if (!TryMakeFacePlane(n, center, out Plane plane))
                    continue;

                Brep faceBrep = face.DuplicateFace(false);
                if (faceBrep == null)
                    continue;

                GetPlaneBounds(faceBrep, plane, out double faceWidth, out double faceHeight);
                double thickness = ThicknessAlong(brep, plane.ZAxis);
                if (faceWidth <= Tol || faceHeight <= Tol || thickness <= Tol)
                    continue;

                UVRect faceRect = FaceUVBounds(faceBrep, plane);
                List<BoundingBox> overlaps = PackedNeighbors.NeighborOverlapsOnFace(boxes, index, n, ContactTol);
                var covered = new List<UVRect>(overlaps.Count);
                for (int o = 0; o < overlaps.Count; o++)
                {
                    if (TryToUV(overlaps[o], plane, faceRect, out UVRect patch))
                        covered.Add(patch);
                }

                bool usableExposed = TryLargestUncovered(faceRect, covered, out UVRect empty)
                    && empty.Width + 1e-9 >= minExposed
                    && empty.Height + 1e-9 >= minExposed;

                bool vertical = Math.Abs(n * Vector3d.ZAxis) <= 0.5;
                bool top = n * Vector3d.ZAxis > 0.5;
                int rank;
                if (usableExposed && vertical)
                    rank = 0;
                else if (usableExposed && top)
                    rank = 1;
                else if (vertical)
                    rank = 2;
                else
                    rank = 3;

                Point3d origin = center;
                double width = faceWidth;
                double height = faceHeight;
                if (usableExposed)
                {
                    origin = plane.PointAt(0.5 * (empty.U0 + empty.U1), 0.5 * (empty.V0 + empty.V1));
                    width = empty.Width;
                    height = empty.Height;
                }

                if (!TryMakeFacePlane(n, origin, out Plane placed))
                    continue;

                list.Add(new LabelCandidate
                {
                    Plane = placed,
                    Width = width,
                    Height = height,
                    Thickness = thickness,
                    Rank = rank,
                    ExposedArea = usableExposed ? empty.Area : 0.0,
                    FaceArea = amp.Area,
                    Center = origin
                });
            }

            list.Sort(CompareCandidates);
            return list;
        }


        static int CompareCandidates(LabelCandidate a, LabelCandidate b)
        {
            int byRank = a.Rank.CompareTo(b.Rank);
            if (byRank != 0)
                return byRank;

            if (a.Rank <= 1)
            {
                int byExposed = b.ExposedArea.CompareTo(a.ExposedArea);
                if (byExposed != 0)
                    return byExposed;
            }

            int byFace = b.FaceArea.CompareTo(a.FaceArea);
            if (byFace != 0)
                return byFace;

            if (a.Center.X > b.Center.X + 1e-9)
                return -1;
            if (a.Center.X < b.Center.X - 1e-9)
                return 1;
            if (a.Center.Y > b.Center.Y + 1e-9)
                return -1;
            if (a.Center.Y < b.Center.Y - 1e-9)
                return 1;
            return 0;
        }


        static bool TryMakeFacePlane(Vector3d normal, Point3d origin, out Plane plane)
        {
            plane = Plane.Unset;
            Vector3d zAxis = normal;
            if (!zAxis.Unitize())
                return false;

            Vector3d yAxis = Vector3d.ZAxis - zAxis * (Vector3d.ZAxis * zAxis);
            if (!yAxis.Unitize())
            {
                yAxis = Vector3d.YAxis - zAxis * (Vector3d.YAxis * zAxis);
                if (!yAxis.Unitize())
                {
                    yAxis = Vector3d.XAxis - zAxis * (Vector3d.XAxis * zAxis);
                    if (!yAxis.Unitize())
                        return false;
                }
            }

            Vector3d xAxis = Vector3d.CrossProduct(yAxis, zAxis);
            if (!xAxis.Unitize())
                return false;
            yAxis = Vector3d.CrossProduct(zAxis, xAxis);
            if (!yAxis.Unitize())
                return false;

            plane = new Plane(origin, xAxis, yAxis);
            return true;
        }


        static UVRect FaceUVBounds(Brep faceBrep, Plane plane)
        {
            GetPlaneBounds(faceBrep, plane, out double width, out double height);
            double hu = 0.5 * width;
            double hv = 0.5 * height;

            double minU = double.MaxValue;
            double maxU = double.MinValue;
            double minV = double.MaxValue;
            double maxV = double.MinValue;
            BoundingBox box = faceBrep.GetBoundingBox(true);
            if (box.IsValid)
            {
                Point3d[] corners = box.GetCorners();
                for (int i = 0; i < corners.Length; i++)
                {
                    plane.ClosestParameter(corners[i], out double u, out double v);
                    if (u < minU) minU = u;
                    if (u > maxU) maxU = u;
                    if (v < minV) minV = v;
                    if (v > maxV) maxV = v;
                }
            }

            if (minU > maxU)
            {
                minU = -hu;
                maxU = hu;
                minV = -hv;
                maxV = hv;
            }

            return new UVRect { U0 = minU, U1 = maxU, V0 = minV, V1 = maxV };
        }


        static bool TryToUV(BoundingBox world, Plane plane, UVRect face, out UVRect patch)
        {
            patch = new UVRect();
            if (!world.IsValid)
                return false;

            double minU = double.MaxValue;
            double maxU = double.MinValue;
            double minV = double.MaxValue;
            double maxV = double.MinValue;
            Point3d[] corners = world.GetCorners();
            for (int i = 0; i < corners.Length; i++)
            {
                plane.ClosestParameter(corners[i], out double u, out double v);
                if (u < minU) minU = u;
                if (u > maxU) maxU = u;
                if (v < minV) minV = v;
                if (v > maxV) maxV = v;
            }

            minU = Math.Max(minU, face.U0);
            maxU = Math.Min(maxU, face.U1);
            minV = Math.Max(minV, face.V0);
            maxV = Math.Min(maxV, face.V1);
            if (maxU - minU <= Tol || maxV - minV <= Tol)
                return false;

            patch = new UVRect { U0 = minU, U1 = maxU, V0 = minV, V1 = maxV };
            return true;
        }


        static bool TryLargestUncovered(UVRect face, List<UVRect> covered, out UVRect empty)
        {
            empty = face;
            if (covered == null || covered.Count == 0)
                return true;

            var us = new List<double>();
            var vs = new List<double>();
            AddUnique(us, face.U0);
            AddUnique(us, face.U1);
            AddUnique(vs, face.V0);
            AddUnique(vs, face.V1);
            for (int i = 0; i < covered.Count; i++)
            {
                UVRect c = covered[i];
                AddUnique(us, Math.Max(face.U0, Math.Min(face.U1, c.U0)));
                AddUnique(us, Math.Max(face.U0, Math.Min(face.U1, c.U1)));
                AddUnique(vs, Math.Max(face.V0, Math.Min(face.V1, c.V0)));
                AddUnique(vs, Math.Max(face.V0, Math.Min(face.V1, c.V1)));
            }

            us.Sort();
            vs.Sort();
            int nu = us.Count - 1;
            int nv = vs.Count - 1;
            if (nu <= 0 || nv <= 0)
                return false;

            var blocked = new bool[nu, nv];
            for (int i = 0; i < nu; i++)
            {
                for (int j = 0; j < nv; j++)
                {
                    double cu = 0.5 * (us[i] + us[i + 1]);
                    double cv = 0.5 * (vs[j] + vs[j + 1]);
                    for (int k = 0; k < covered.Count; k++)
                    {
                        UVRect c = covered[k];
                        if (cu >= c.U0 - 1e-12 && cu <= c.U1 + 1e-12 && cv >= c.V0 - 1e-12 && cv <= c.V1 + 1e-12)
                        {
                            blocked[i, j] = true;
                            break;
                        }
                    }
                }
            }

            double bestArea = -1;
            bool any = false;
            UVRect best = face;
            for (int i0 = 0; i0 < nu; i0++)
            {
                var freeCol = new bool[nv];
                for (int j = 0; j < nv; j++)
                    freeCol[j] = true;

                for (int i1 = i0; i1 < nu; i1++)
                {
                    for (int j = 0; j < nv; j++)
                        freeCol[j] = freeCol[j] && !blocked[i1, j];

                    int run = 0;
                    for (int j = 0; j <= nv; j++)
                    {
                        bool free = j < nv && freeCol[j];
                        if (free)
                        {
                            run++;
                            continue;
                        }

                        if (run > 0)
                        {
                            int j0 = j - run;
                            double area = (us[i1 + 1] - us[i0]) * (vs[j] - vs[j0]);
                            if (area > bestArea)
                            {
                                bestArea = area;
                                best = new UVRect { U0 = us[i0], U1 = us[i1 + 1], V0 = vs[j0], V1 = vs[j] };
                                any = true;
                            }
                        }

                        run = 0;
                    }
                }
            }

            if (!any || best.Area <= Tol)
                return false;

            empty = best;
            return true;
        }


        static void AddUnique(List<double> values, double value)
        {
            for (int i = 0; i < values.Count; i++)
            {
                if (Math.Abs(values[i] - value) <= 1e-9)
                    return;
            }

            values.Add(value);
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
