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
    public class StaggerColumn_GH : GH_Component
    {
        public StaggerColumn_GH()
          : base("Stagger Column", "StaggerCol",
              "Packs offcuts with the longest side on Z. At each lowest solid height, takes the longest scrap whose top misses every side neighbor's top by at least St. A piece that would bridge a hole is left out.",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
            pManager.AddBoxParameter("Box", "B", "Box container to fill with Offcuts", GH_ParamAccess.item);
            pManager.AddNumberParameter("Stagger", "St", "Minimum distance between a scrap's top and the top of a piece it touches on the side. Default 4.", GH_ParamAccess.item, 4.0);

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
            double stagger = 4.0;

            if (!DA.GetDataList(0, offcutData))
                return;
            if (!DA.GetData(1, ref boundingBox))
                return;
            DA.GetData(2, ref stagger);

            if (stagger < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Stagger was below 0, so 0 is used.");
                stagger = 0;
            }

            Box originBox = PackedOffcuts.OriginBox(boundingBox);
            var container = PackedOffcuts.MakeContainer(boundingBox);
            List<CromulentBisgetti.ContainerPacking.Entities.Item> packItems = PackedOffcuts.MakeItems(offcutData);
            StaggerPack result = AxisLockPacker.PackStagger(container, packItems, (decimal)stagger);

            if (result.UniformLengths)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Ends could not stagger; every scrap has the same length.");
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{result.Placed.Count} of {offcutData.Count} offcut(s) packed. {result.AlignedSideJoints} side joints still end within {PackedOffcuts.Inches((decimal)stagger)}.");

            PackedOffcuts.Write(DA, originBox, PackedOffcuts.FromItems(result.Placed, offcutData));
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_BinPackingCS;

        public override Guid ComponentGuid => new Guid("6F2A9C14-8B37-4E51-A6D0-3C91F7E24B58");
    }
}
