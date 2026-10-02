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
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class DowelColumn_GH : GH_Component
    {
        public DowelColumn_GH()
          : base("Dowel Column", "DowelCol",
              "Places Offcuts inside a column so each piece contains vertical dowels. The box is only a boundary. N is the dowel count (1 sits on the center; 2 or more sit on an inset polygon). Each piece must contain at least two dowels, or the single dowel when N is 1. Pieces that share a dowel lap in Z.",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "Pieces to place (X, Y, Z). Axis rotations are tried. A piece that cannot cover the required dowels is left unused.", GH_ParamAccess.list);
            pManager.AddBoxParameter("Box", "B", "Column boundary. Wood and dowels stay inside this box.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Dowel Count", "N", "How many vertical dowels. 1 is the section center. 2 or more are a regular polygon inset from the faces. Ignored when Dowel Points is connected.", GH_ParamAccess.item, 3);
            pManager.AddPointParameter("Dowel Points", "P", "Optional dowel locations. Each point is one vertical dowel through the full column height. The point's height is ignored. When any point is inside the section, N is not used.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lap", "L", "Shared height where two pieces on the same dowel overlap. That band is shared volume. Default 1.", GH_ParamAccess.item, 1.0);

            pManager[3].Optional = true;

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Placed Offcuts", "POc", "Solids inside the column, in placement order.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Offcuts", "Oc", "Placed pieces as Offcuts (index, rotated size, geometry, end planes). Same order as POc.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Dowels", "Ln", "Vertical dowel lines through the column.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Unused Offcuts", "UOc", "Stock that was not placed, in input order.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Dowel Index", "D", "Branch i lists the dowel indices inside placed piece i.", GH_ParamAccess.tree);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var stock = new List<Offcut>();
            Box box = Box.Unset;
            int count = 3;
            var points = new List<Point3d>();
            double lap = 1.0;

            if (!DA.GetDataList(0, stock))
                return;
            if (!DA.GetData(1, ref box))
                return;
            DA.GetData(2, ref count);
            DA.GetDataList(3, points);
            DA.GetData(4, ref lap);

            stock.RemoveAll(item => item == null);

            if (!box.IsValid || box.X.Length <= DowelColumn.Tolerance || box.Y.Length <= DowelColumn.Tolerance || box.Z.Length <= DowelColumn.Tolerance)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Column box has no volume.");
                return;
            }

            if (lap < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Lap was below 0 and was set to 0.");
                lap = 0;
            }

            double sizeX = box.X.Length;
            double sizeY = box.Y.Length;
            double sizeZ = box.Z.Length;
            bool fromPoints = false;
            List<DowelColumn.Dowel> dowels = ReadPoints(box, points, out int skipped);

            if (skipped > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{skipped} dowel point(s) sit outside the column section and were skipped.");
            }

            if (dowels.Count > 0)
            {
                fromPoints = true;
            }
            else
            {
                if (points.Count > 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "No dowel point landed in the column section. Using the dowel count instead.");
                }

                if (count < 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Dowel count was below 1 and was set to 1.");
                    count = 1;
                }
                else if (count > DowelColumn.MaxGeneratedCount)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Dowel count was capped at {DowelColumn.MaxGeneratedCount}.");
                    count = DowelColumn.MaxGeneratedCount;
                }

                dowels = DowelColumn.RegularPolygon(count, sizeX, sizeY);
            }

            var lines = new List<Curve>(dowels.Count);
            for (int i = 0; i < dowels.Count; i++)
            {
                Point3d a = ToWorld(box, dowels[i].X, dowels[i].Y, 0);
                Point3d b = ToWorld(box, dowels[i].X, dowels[i].Y, sizeZ);
                lines.Add(new LineCurve(new Line(a, b)));
            }

            DA.SetDataList(2, lines);

            if (stock.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No Offcuts provided.");
                DA.SetDataList(0, new List<Brep>());
                DA.SetDataList(1, new List<Offcut_GH>());
                DA.SetDataList(3, new List<Offcut_GH>());
                DA.SetDataTree(4, new DataTree<int>());
                return;
            }

            var xs = new double[stock.Count];
            var ys = new double[stock.Count];
            var zs = new double[stock.Count];
            for (int i = 0; i < stock.Count; i++)
            {
                xs[i] = stock[i].X;
                ys[i] = stock[i].Y;
                zs[i] = stock[i].Z;
            }

            List<DowelColumn.Placement> placed = DowelColumn.Place(xs, ys, zs, sizeX, sizeY, sizeZ, dowels, lap);

            var breps = new List<Brep>(placed.Count);
            var offcuts = new List<Offcut_GH>(placed.Count);
            var hits = new DataTree<int>();
            var used = new bool[stock.Count];

            for (int i = 0; i < placed.Count; i++)
            {
                DowelColumn.Placement piece = placed[i];
                used[piece.Source] = true;
                Offcut source = stock[piece.Source];

                if (!TryBuild(box, source, piece, i, out Brep solid, out Offcut offcut))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Offcut {source.Index} was placed but its solid could not be built.");
                    continue;
                }

                breps.Add(solid);
                offcuts.Add(new Offcut_GH(offcut));
                hits.AddRange(piece.Hits, new GH_Path(breps.Count - 1));
            }

            var unused = new List<Offcut_GH>();
            for (int i = 0; i < stock.Count; i++)
            {
                if (!used[i])
                    unused.Add(new Offcut_GH(stock[i]));
            }

            DA.SetDataList(0, breps);
            DA.SetDataList(1, offcuts);
            DA.SetDataList(3, unused);
            DA.SetDataTree(4, hits);

            string sourceName = fromPoints ? "points" : "count";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"Placed {breps.Count} of {stock.Count} offcut(s) on {dowels.Count} dowel(s) ({sourceName}), lap {lap}.");

            if (breps.Count == 0)
            {
                int need = DowelColumn.RequiredHits(dowels.Count);
                if (need < 2)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "No offcut contained the dowel and fit inside the column.");
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "No offcut covered two dowels inside the column. Each plan side must be at least as long as that dowel pair's X separation and Y separation.");
                }
            }
        }


        static List<DowelColumn.Dowel> ReadPoints(Box box, List<Point3d> points, out int skipped)
        {
            var dowels = new List<DowelColumn.Dowel>();
            skipped = 0;
            if (points == null || points.Count == 0)
                return dowels;

            Plane plane = box.Plane;
            double sizeX = box.X.Length;
            double sizeY = box.Y.Length;

            for (int i = 0; i < points.Count; i++)
            {
                if (!plane.ClosestParameter(points[i], out double u, out double v))
                {
                    skipped++;
                    continue;
                }

                double x = u - box.X.Min;
                double y = v - box.Y.Min;
                if (x < -DowelColumn.Tolerance || x > sizeX + DowelColumn.Tolerance
                    || y < -DowelColumn.Tolerance || y > sizeY + DowelColumn.Tolerance)
                {
                    skipped++;
                    continue;
                }

                dowels.Add(new DowelColumn.Dowel
                {
                    X = Clamp(x, 0, sizeX),
                    Y = Clamp(y, 0, sizeY)
                });
            }

            return dowels;
        }


        static bool TryBuild(Box box, Offcut source, DowelColumn.Placement piece, int order, out Brep solid, out Offcut offcut)
        {
            solid = null;
            offcut = null;

            Point3d origin = ToWorld(box, piece.X, piece.Y, piece.Z);
            Plane frame = new Plane(origin, box.Plane.XAxis, box.Plane.YAxis);
            var footprint = new Rectangle3d(frame, piece.Dx, piece.Dy);
            Vector3d up = box.Plane.ZAxis * piece.Dz;

            Surface extrusion = Surface.CreateExtrusion(footprint.ToNurbsCurve(), up);
            if (extrusion == null)
                return false;

            Brep open = extrusion.ToBrep();
            Brep closed = open.CapPlanarHoles(0.0001);
            if (closed == null)
                return false;

            closed.Faces.SplitKinkyFaces(0.0001);
            if (closed.SolidOrientation == BrepSolidOrientation.Inward)
                closed.Flip();

            Point3d bottom = ToWorld(box, piece.X + piece.Dx * 0.5, piece.Y + piece.Dy * 0.5, piece.Z);
            Point3d top = ToWorld(box, piece.X + piece.Dx * 0.5, piece.Y + piece.Dy * 0.5, piece.Z + piece.Dz);
            Point3d mid = ToWorld(box, piece.X + piece.Dx * 0.5, piece.Y + piece.Dy * 0.5, piece.Z + piece.Dz * 0.5);
            Plane end = new Plane(bottom, box.Plane.XAxis, box.Plane.YAxis);
            Plane second = new Plane(top, box.Plane.XAxis, box.Plane.YAxis);
            Plane average = new Plane(mid, box.Plane.XAxis, box.Plane.YAxis);
            double volume = piece.Dx * piece.Dy * piece.Dz;

            solid = closed;
            offcut = Offcut.CreateOffcut(
                closed,
                source.Index,
                piece.Dx,
                piece.Dy,
                piece.Dz,
                source.Vol > 0 ? source.Vol : volume,
                volume,
                end,
                second,
                average,
                average,
                frame,
                order);

            return true;
        }


        static Point3d ToWorld(Box box, double x, double y, double z)
        {
            Plane plane = box.Plane;
            return plane.Origin
                + plane.XAxis * (box.X.Min + x)
                + plane.YAxis * (box.Y.Min + y)
                + plane.ZAxis * (box.Z.Min + z);
        }


        static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_BinPackingCS;

        public override Guid ComponentGuid => new Guid("C4A91E72-6B38-4F05-9D14-2E7B8A0C5F61");
    }
}
