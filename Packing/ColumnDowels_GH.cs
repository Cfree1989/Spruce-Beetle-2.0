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
using Grasshopper.Kernel.Parameters;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class ColumnDowels_GH : GH_Component
    {
        public ColumnDowels_GH()
          : base("Dowel Column", "DowelCol",
              "Ranks horizontal dowels through a packed column. Each dowel is a cylinder perpendicular to world Z, through the widest face, and may cross empty spans. N keeps the lines that pierce the most pieces. A line drops just far enough to enter another board when the boards it already crosses still contain the hole.",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Packed Offcuts", "Oc", "Offcuts from Bin Packing EB-AFIT", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Count", "N", "How many dowels to keep. Lines that pierce more pieces come first.", GH_ParamAccess.item, 4);
            pManager.AddNumberParameter("Diameter", "Dia", "Dowel diameter. The drilled hole is this plus two clearances.", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Clearance", "Cl", "Gap on each side between the dowel and the hole. Default 0.005. The hole is Dia plus two of these.", GH_ParamAccess.item, ClearanceSlider.Default);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Dowels", "D", "Dowel cylinders at Dia, from the outer face of the first piece to the outer face of the last.", GH_ParamAccess.list);
            pManager.AddCurveParameter("Lines", "Ln", "Dowel axes. Horizontal, along X or along Y. Preview only; baking this component skips these curves", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Score", "S", "How many pieces each dowel pierces.", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Pieces", "Pi", "Branch i lists the packed indices dowel i passes through, in order along the dowel.", GH_ParamAccess.tree);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var packed = new List<Offcut>();
            int count = 4;
            double diameter = 0.5;
            double clearance = ClearanceSlider.Default;

            if (!DA.GetDataList(0, packed))
                return;
            DA.GetData(1, ref count);
            DA.GetData(2, ref diameter);
            DA.GetData(3, ref clearance);

            packed.RemoveAll(item => item == null);
            clearance = ClearanceSlider.Read(this, clearance);

            if (count < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Count was below 1, so no dowels are shown.");
                count = 0;
            }

            if (diameter <= ColumnDowels.Tolerance)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Diameter must be greater than 0.");
                return;
            }

            var boxes = new List<ColumnDowels.Box>(packed.Count);
            var indexOf = new List<int>(packed.Count);
            int missing = 0;
            for (int i = 0; i < packed.Count; i++)
            {
                if (!TryBox(packed[i], out ColumnDowels.Box box))
                {
                    missing++;
                    continue;
                }

                boxes.Add(box);
                indexOf.Add(i);
            }

            if (missing > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{missing} offcut(s) had no solid and were skipped.");
            }

            ColumnDowels.Rank rank = ColumnDowels.Select(boxes, count, diameter, clearance);
            var dowels = new List<Brep>(rank.Chosen.Count);
            var lines = new List<Curve>(rank.Chosen.Count);
            var scores = new List<int>(rank.Chosen.Count);
            var pieces = new DataTree<int>();

            for (int i = 0; i < rank.Chosen.Count; i++)
            {
                ColumnDowels.Dowel line = rank.Chosen[i];
                ColumnDowels.Ends(line, out double x0, out double y0, out double z0, out double x1, out double y1, out double z1);
                var start = new Point3d(x0, y0, z0);
                var end = new Point3d(x1, y1, z1);
                if (!TryCylinder(start, end, diameter, out Brep solid))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Dowel {i} could not be built.");
                    continue;
                }

                dowels.Add(solid);
                lines.Add(new LineCurve(start, end));
                scores.Add(line.Score);

                var path = new GH_Path(dowels.Count - 1);
                for (int p = 0; p < line.Pieces.Length; p++)
                    pieces.Add(indexOf[line.Pieces[p]], path);
            }

            string tieNote = rank.TieIns > 0
                ? $" {rank.TieIns} stop at the far face of a piece that already has a dowel."
                : "";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{rank.Ranked} line(s) ranked, showing {dowels.Count}. Skipped {rank.Collided} that hit a chosen dowel.{tieNote}");

            if (boxes.Count > 0 && rank.Ranked == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "No dowel fit. A piece is used only when its widest face is vertical, and the shared footprint must hold the hole (diameter plus two clearances).");
            }

            DA.SetDataList(0, dowels);
            DA.SetDataList(1, lines);
            DA.SetDataList(2, scores);
            DA.SetDataTree(3, pieces);
        }


        static bool TryBox(Offcut offcut, out ColumnDowels.Box box)
        {
            box = new ColumnDowels.Box();
            if (offcut?.OffcutGeometry == null)
                return false;

            BoundingBox bounds = offcut.OffcutGeometry.GetBoundingBox(true);
            if (!bounds.IsValid)
                return false;

            box.X0 = bounds.Min.X;
            box.Y0 = bounds.Min.Y;
            box.Z0 = bounds.Min.Z;
            box.X1 = bounds.Max.X;
            box.Y1 = bounds.Max.Y;
            box.Z1 = bounds.Max.Z;
            return box.X1 - box.X0 > ColumnDowels.Tolerance
                && box.Y1 - box.Y0 > ColumnDowels.Tolerance
                && box.Z1 - box.Z0 > ColumnDowels.Tolerance;
        }


        static bool TryCylinder(Point3d start, Point3d end, double diameter, out Brep solid)
        {
            solid = null;
            Vector3d axis = end - start;
            if (axis.Length <= ColumnDowels.Tolerance || !axis.Unitize())
                return false;

            var plane = new Plane(start, axis);
            var cylinder = new Cylinder(new Circle(plane, diameter * 0.5), start.DistanceTo(end));
            solid = cylinder.ToBrep(true, true);
            return solid != null && solid.IsValid;
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
                if (param is Param_Plane || param is Param_Line || param is Param_Curve)
                    continue;
                if (param is IGH_BakeAwareObject baker)
                    baker.BakeGeometry(doc, att, obj_ids);
            }
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_FindIntersections;

        public override Guid ComponentGuid => new Guid("E8C4B1A6-3D72-4F58-9A14-7B6E0C5D2F93");
    }
}
