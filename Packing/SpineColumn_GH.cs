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


namespace SpruceBeetle.Packing
{
    public class SpineColumn_GH : GH_Component
    {
        public SpineColumn_GH()
          : base("Spine Column", "SpineCol",
              "Packs offcuts with the longest side on Z. Places N long scraps on the floor first, spread to opposite corners, then fills the rest longest-first. A spine is at least 3/4 of the column height when the pile has one. A piece that would bridge a hole is left out.",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
            pManager.AddBoxParameter("Box", "B", "Box container to fill with Offcuts", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Spines", "N", "How many long scraps to stand on the floor first. Clamped to 1–4. Default 2.", GH_ParamAccess.item, 2);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Packed Offcuts", "POc", "List of packed Offcuts as solids", GH_ParamAccess.list);
            pManager.AddBrepParameter("Container", "C", "The container where the Offcuts are packed into", GH_ParamAccess.item);
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed pieces as Offcuts (index, rotated size, geometry, Z-end planes). Feed Packed Contacts, then Select Contacts and Contact Tenon.", GH_ParamAccess.list);

            pManager.HideParameter(1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var offcutData = new List<Offcut>();
            Box boundingBox = new Box();
            int spines = 2;

            if (!DA.GetDataList(0, offcutData))
                return;
            if (!DA.GetData(1, ref boundingBox))
                return;
            DA.GetData(2, ref spines);

            if (spines < 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Spines was below 1, so 1 is used.");
                spines = 1;
            }
            else if (spines > 4)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Spines was above 4, so 4 is used.");
                spines = 4;
            }

            Box originBox = PackedOffcuts.OriginBox(boundingBox);
            var container = PackedOffcuts.MakeContainer(boundingBox);
            List<CromulentBisgetti.ContainerPacking.Entities.Item> packItems = PackedOffcuts.MakeItems(offcutData);
            SpinePack result = AxisLockPacker.PackSpines(container, packItems, spines);

            if (result.ShortSpineLengths.Count == 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"1 spine is shorter than 3/4 of the column ({PackedOffcuts.Inches(result.MinimumSpineLength)}): {PackedOffcuts.Inches(result.ShortSpineLengths)}.");
            }
            else if (result.ShortSpineLengths.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{result.ShortSpineLengths.Count} spines are shorter than 3/4 of the column ({PackedOffcuts.Inches(result.MinimumSpineLength)}): {PackedOffcuts.Inches(result.ShortSpineLengths)}.");
            }

            if (result.SpineLengths.Count < result.Requested)
            {
                string fit = offcutData.Count > result.SpineLengths.Count
                    ? " The others did not fit on the floor."
                    : "";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Placed {result.SpineLengths.Count} of {result.Requested} spines.{fit}");
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"Placed {result.SpineLengths.Count} spines ({PackedOffcuts.Inches(result.SpineLengths)}). {result.Placed.Count} of {offcutData.Count} offcut(s) packed.");

            PackedOffcuts.Write(DA, originBox, PackedOffcuts.FromItems(result.Placed, offcutData));
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_BinPackingCS;

        public override Guid ComponentGuid => new Guid("2D8E5B70-1A64-4C93-9F28-6E0B4A7D15C3");
    }
}
