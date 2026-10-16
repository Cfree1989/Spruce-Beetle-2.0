/*
 * MIT License
 * 
 * Copyright (c) 2019 davidmchapman
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

/*
 * The C# libary used here can be found in this github repository:
 * https://github.com/davidmchapman/3DContainerPacking
 */


using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;
using CromulentBisgetti.ContainerPacking.Algorithms;
using CromulentBisgetti.ContainerPacking.Entities;
using CromulentBisgetti.ContainerPacking;


namespace SpruceBeetle.Packing
{
    public class BinPackingCS_GH : GH_Component
    {
        IGH_Param orientationParam = null;

        public BinPackingCS_GH()
          : base("Bin Packing EB-AFIT", "PackBin", "Packs Offcuts into a box with EB-AFIT. Orientation is Unlimited, Longest Z, or Shortest Z.",
              "Spruce Beetle", "   Packing")
        {
            ApplyDisplayNames();
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcut Data", "OcD", "List of dimensions of all the Offcuts", GH_ParamAccess.list);
            pManager.AddBoxParameter("Box", "B", "Box container to fill with Offcuts", GH_ParamAccess.item);
            pManager.AddTextParameter("Orientation", "Or", "Unlimited (any rotation), Longest Z, or Shortest Z (thickness on Z). Locked modes sit each piece on the floor or a complete platform, so a piece does not bridge a hole. The other two sides may still swap in plan.", GH_ParamAccess.item, "Unlimited");
            pManager[2].Optional = true;
            orientationParam = pManager[2];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Packed Offcuts", "POc", "List of packed Offcuts as solids", GH_ParamAccess.list);
            pManager.AddBrepParameter("Container", "C", "The container where the Offcuts are packed into", GH_ParamAccess.item);
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed pieces as Offcuts (index, rotated size, geometry, Z-end planes). Feed Packed Contacts, then Select Contacts and Contact Tenon.", GH_ParamAccess.list);

            pManager.HideParameter(1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> offcutData = new List<Offcut>();
            Box boundingBox = new Box();
            string orientation = "Unlimited";

            // access input parameters
            if(!DA.GetDataList(0, offcutData)) return;
            if (!DA.GetData(1, ref boundingBox)) return;
            DA.GetData(2, ref orientation);

            Box originBox = PackedOffcuts.OriginBox(boundingBox);
            List<Offcut> packedOffcuts = PackOffcuts(boundingBox, offcutData, orientation);
            PackedOffcuts.Write(DA, originBox, packedOffcuts);
        }


        //------------------------------------------------------------
        // PackOffcuts
        //------------------------------------------------------------
        protected List<Offcut> PackOffcuts(Box boundingBox, List<Offcut> offcutData, string orientation)
        {
            Container container = PackedOffcuts.MakeContainer(boundingBox);
            List<Item> packItems = PackedOffcuts.MakeItems(offcutData);

            bool longestOnZ;
            string mode = ResolveOrientation(orientation, out longestOnZ);
            List<Item> packedItems;

            if (mode == "Unlimited")
            {
                // create a list of Algorithms and specify Algorithm
                List<int> algorithm = new List<int>
                {
                    (int)AlgorithmType.EB_AFIT
                };

                // call bin packing method
                List<ContainerPackingResult> results = PackingService.Pack(new List<Container> { container }, packItems, algorithm);

                var pkdItems = results[0].AlgorithmPackingResults;
                packedItems = pkdItems[0].PackedItems;
            }
            else
            {
                packedItems = AxisLockPacker.Pack(container, packItems, longestOnZ);
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{packedItems.Count} of {offcutData.Count} offcut(s) packed ({mode}).");

            return PackedOffcuts.FromItems(packedItems, offcutData);
        }


        protected override void BeforeSolveInstance()
        {
            if (orientationParam == null)
                return;

            GH_ValueList list = null;
            foreach (var source in orientationParam.Sources)
            {
                if (source is GH_ValueList vl)
                {
                    list = vl;
                    break;
                }
            }

            if (list == null)
            {
                if (orientationParam.Sources.Count > 0)
                    return;
                if (Instances.ActiveCanvas?.Document == null)
                    return;

                list = new GH_ValueList();
                list.CreateAttributes();
                list.Attributes.Pivot = new System.Drawing.PointF(Attributes.Pivot.X - 220, Attributes.Pivot.Y + 42);
                Instances.ActiveCanvas.Document.AddObject(list, false);
                orientationParam.AddSource(list);
            }

            SyncOrientationList(list);
            orientationParam.CollectData();
        }


        private static void SyncOrientationList(GH_ValueList list)
        {
            string[] wanted = { "Unlimited", "Longest Z", "Shortest Z" };
            if (list.ListItems.Count == wanted.Length)
            {
                bool same = true;
                for (int i = 0; i < wanted.Length; i++)
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
            foreach (string mode in wanted)
                list.ListItems.Add(new GH_ValueListItem(mode, $"\"{mode}\""));
        }


        private string ResolveOrientation(string orientation, out bool longestOnZ)
        {
            string key = (orientation ?? string.Empty).Trim();
            if (string.Equals(key, "Longest Z", StringComparison.OrdinalIgnoreCase))
            {
                longestOnZ = true;
                return "Longest Z";
            }

            if (string.Equals(key, "Shortest Z", StringComparison.OrdinalIgnoreCase))
            {
                longestOnZ = false;
                return "Shortest Z";
            }

            longestOnZ = false;
            if (key.Length > 0 && !string.Equals(key, "Unlimited", StringComparison.OrdinalIgnoreCase))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Unknown orientation '{key}'; using Unlimited.");
            }

            return "Unlimited";
        }


        public override bool Read(GH_IReader reader)
        {
            bool ok = base.Read(reader);
            ApplyDisplayNames();
            return ok;
        }


        public override void AddedToDocument(GH_Document document)
        {
            ApplyDisplayNames();
            base.AddedToDocument(document);
        }


        void ApplyDisplayNames()
        {
            Name = "Bin Packing EB-AFIT";
            NickName = "PackBin";
            Description = "Packs Offcuts into a box with EB-AFIT. Orientation is Unlimited, Longest Z, or Shortest Z.";
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_BinPackingCS;

        // component giud
        public override Guid ComponentGuid => new Guid("99C99B34-2B2F-418D-AB51-F3A139064C10");
    }
}