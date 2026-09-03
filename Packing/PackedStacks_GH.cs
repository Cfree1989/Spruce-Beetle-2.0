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
    public class PackedStacks_GH : GH_Component
    {
        public PackedStacks_GH()
          : base("Packed Stacks", "PackStacks",
              "Group packed Offcuts that touch face-to-face along Z, ordered bottom to top, so Tenon Joints can run on each stack",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Packed Offcuts", "Oc", "Offcuts from Bin Packing EB-AFIT", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "Maximum gap to treat as a Z-face contact", GH_ParamAccess.item, 0.01);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Stacks", "S", "Data tree of Offcuts: one branch per Z-stack (2 or more pieces), bottom to top. Graft into Tenon Joints.", GH_ParamAccess.tree);
            pManager.AddGenericParameter("Isolated", "I", "Packed Offcuts with no Z neighbor", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Contact Planes", "P", "Interface planes between stacked pieces", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var packed = new List<Offcut>();
            double tolerance = 0.01;

            if (!DA.GetDataList(0, packed))
                return;
            DA.GetData(1, ref tolerance);

            if (packed.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No packed Offcuts provided.");
                return;
            }

            if (tolerance < 0)
                tolerance = 0;

            List<List<int>> stacks = PackedNeighbors.ZStacks(packed, tolerance, out List<Plane> contacts);

            var tree = new DataTree<Offcut_GH>();
            var isolated = new List<Offcut_GH>();
            int stackIndex = 0;

            for (int i = 0; i < stacks.Count; i++)
            {
                List<int> members = stacks[i];
                if (members.Count < 2)
                {
                    isolated.Add(new Offcut_GH(packed[members[0]]));
                    continue;
                }

                var path = new GH_Path(stackIndex++);
                for (int j = 0; j < members.Count; j++)
                    tree.Add(new Offcut_GH(packed[members[j]]), path);
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{stackIndex} stack(s) with joints, {isolated.Count} isolated, {contacts.Count} Z-contact(s).");

            DA.SetDataTree(0, tree);
            DA.SetDataList(1, isolated);
            DA.SetDataList(2, contacts);
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_FindIntersections;

        public override Guid ComponentGuid => new Guid("8F3A6C21-4B9E-4D17-9A55-E2C8B1F04673");
    }
}
