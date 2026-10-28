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
    public class BondColumn_GH : GH_Component
    {
        public BondColumn_GH()
          : base("Bond Column", "BondCol",
              "Packs offcuts with the longest side on Z and every thickness on the same plan axis, so the wide faces stack in layers that horizontal dowels can run through. A board's side edges stay at least Bo from the seams in the layers it touches (a running bond in plan), and its top misses side neighbors' tops by at least St. Narrower scraps fill the edge gaps the offset leaves. A piece that would bridge a hole is left out.",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
            pManager.AddBoxParameter("Box", "B", "Box container to fill with Offcuts", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stagger", "St", "Minimum distance between a scrap's top and the top of a piece it touches on the side. Default 4.", GH_ParamAccess.item, 4.0);
            pManager.AddNumberParameter("Bond", "Bo", "Minimum distance from a board's side edge to a seam in a layer it touches face to face. Also the smallest face contact counted as joined. Default 1.5.", GH_ParamAccess.item, 1.5);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Packed Offcuts", "POc", "List of packed Offcuts as solids", GH_ParamAccess.list);
            pManager.AddBrepParameter("Container", "C", "The container where the Offcuts are packed into", GH_ParamAccess.item);
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed pieces as Offcuts (index, rotated size, geometry, Z-end planes). Feed Dowel Column or Packed Contacts.", GH_ParamAccess.list);

            pManager.HideParameter(1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var offcutData = new List<Offcut>();
            Box boundingBox = new Box();
            double stagger = 4.0;
            double bond = 1.5;

            if (!DA.GetDataList(0, offcutData))
                return;
            if (!DA.GetData(1, ref boundingBox))
                return;
            DA.GetData(2, ref stagger);
            DA.GetData(3, ref bond);

            if (stagger < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Stagger was below 0, so 0 is used.");
                stagger = 0;
            }
            if (bond < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Bond was below 0, so 0 is used.");
                bond = 0;
            }

            Box originBox = PackedOffcuts.OriginBox(boundingBox);
            var container = PackedOffcuts.MakeContainer(boundingBox);
            List<CromulentBisgetti.ContainerPacking.Entities.Item> packItems = PackedOffcuts.MakeItems(offcutData);
            BondPack result = BondPacker.Pack(container, packItems, stagger, bond);

            if (result.Groups > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"The pack is {result.Groups} separate groups. No wide face of at least {PackedOffcuts.Inches((decimal)bond)} joins them.");
            }

            if (result.UniformLengths)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Ends could not stagger; every scrap has the same length.");
            }

            string axis = result.ThroughX ? "X" : "Y";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{result.Placed.Count} of {offcutData.Count} offcut(s) packed. Thicknesses run along {axis}, so dowels run along {axis}. " +
                $"{result.Groups} group(s) joined through wide faces. " +
                $"{result.LinedUp} board(s) have a side edge within {PackedOffcuts.Inches((decimal)bond)} of a seam in a touching layer. " +
                $"{result.AlignedSideJoints} side joints still end within {PackedOffcuts.Inches((decimal)stagger)}.");

            PackedOffcuts.Write(DA, originBox, PackedOffcuts.FromItems(result.Placed, offcutData));
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_BinPackingCS;

        public override Guid ComponentGuid => new Guid("B4C71E28-5D93-4A6F-8E12-7F3A0D9C6B41");
    }
}
