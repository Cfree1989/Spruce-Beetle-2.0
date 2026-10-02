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
            pManager.AddTextParameter("Orientation", "Or", "Unlimited (any rotation), Longest Z, or Shortest Z (thickness on Z). The other two sides may still swap in plan.", GH_ParamAccess.item, "Unlimited");
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

            // Container at World XY from the origin, using axis *lengths* (T1 is wrong when a domain starts below 0).
            Box originBox = new Box(Plane.WorldXY,
                new Interval(0, boundingBox.X.Length),
                new Interval(0, boundingBox.Y.Length),
                new Interval(0, boundingBox.Z.Length));

            /*  Because kinked surfaces can cause problems down stream, Rhino always splits kinked surfaces when adding Breps to the document.
                Sometimes, we have to do it.    */
            originBox.ToBrep().Faces.SplitKinkyFaces(0.0001);

            List<Offcut> packedOffcuts = PackOffcuts(boundingBox, offcutData, orientation);

            var packedBreps = new List<Brep>(packedOffcuts.Count);
            var packedGH = new List<Offcut_GH>(packedOffcuts.Count);
            for (int i = 0; i < packedOffcuts.Count; i++)
            {
                packedBreps.Add(packedOffcuts[i].OffcutGeometry);
                packedGH.Add(new Offcut_GH(packedOffcuts[i]));
            }

            DA.SetDataList(0, packedBreps);
            DA.SetData(1, originBox);
            DA.SetDataList(2, packedGH);
        }


        //------------------------------------------------------------
        // PackOffcuts
        //------------------------------------------------------------
        protected List<Offcut> PackOffcuts(Box boundingBox, List<Offcut> offcutData, string orientation)
        {
            // box dimensions
            double xB = boundingBox.X.Length;
            decimal xBB = (decimal)xB;

            double yB = boundingBox.Y.Length;
            decimal yBB = (decimal)yB;

            double zB = boundingBox.Z.Length;
            decimal zBB = (decimal)zB;

            // create Containers
            List<Container> containers = new List<Container>
            {
                new Container(0, xBB, yBB, zBB)
            };

            // create list of Items to pack
            List<Item> packItems = new List<Item>();

            for (int i = 0; i < offcutData.Count; i++)
            {
                double xI = offcutData[i].X;
                decimal x = (decimal)xI;

                double yI = offcutData[i].Y;
                decimal y = (decimal)yI;

                double zI = offcutData[i].Z;
                decimal z = (decimal)zI;

                packItems.Add(new Item(i, x, y, z, 1));
            }

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
                List<ContainerPackingResult> results = PackingService.Pack(containers, packItems, algorithm);

                var pkdItems = results[0].AlgorithmPackingResults;
                packedItems = pkdItems[0].PackedItems;
            }
            else
            {
                AlgorithmPackingResult locked = new EB_AFIT_AxisLock(longestOnZ).Run(containers[0], packItems);
                packedItems = locked.PackedItems ?? new List<Item>();
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{packedItems.Count} of {offcutData.Count} offcut(s) packed ({mode}).");

            var packedOffcuts = new List<Offcut>();

            // EB-AFIT is a pallet packer: Length→X, Height→Y, Width→Z.
            // Rhino/architecture uses Z-up, so swap library Y (height) with Z (width).
            for ( int i = 0; i < packedItems.Count; i++)
            {
                double ocX = (double)packedItems[i].PackDimX;
                double ocY = (double)packedItems[i].PackDimZ;
                double ocZ = (double)packedItems[i].PackDimY;

                double cX = (double)packedItems[i].CoordX;
                double cY = (double)packedItems[i].CoordZ;
                double cZ = (double)packedItems[i].CoordY;

                Point3d basePt = new Point3d(cX, cY, cZ);
                Vector3d normalVec = new Vector3d(0, 0, ocZ);

                Plane basePlane = new Plane(basePt, Vector3d.ZAxis);

                Rectangle3d baseRect = new Rectangle3d(basePlane, ocX, ocY);

                Brep openOffcut = Surface.CreateExtrusion(baseRect.ToNurbsCurve(), normalVec).ToBrep();
                Brep closedOffcut = openOffcut.CapPlanarHoles(0.0001);

                /*  Because kinked surfaces can cause problems down stream, Rhino always splits kinked surfaces when adding Breps to the document.
                    Sometimes, we have to do it.    */
                closedOffcut.Faces.SplitKinkyFaces(0.0001);

                if (BrepSolidOrientation.Inward == closedOffcut.SolidOrientation)
                    closedOffcut.Flip();

                int sourceId = packedItems[i].ID;
                Offcut source = (sourceId >= 0 && sourceId < offcutData.Count)
                    ? offcutData[sourceId]
                    : new Offcut(sourceId, ocX, ocY, ocZ);

                Point3d bottom = new Point3d(cX + ocX * 0.5, cY + ocY * 0.5, cZ);
                Point3d top = new Point3d(cX + ocX * 0.5, cY + ocY * 0.5, cZ + ocZ);
                Point3d mid = new Point3d(cX + ocX * 0.5, cY + ocY * 0.5, cZ + ocZ * 0.5);

                Plane firstPlane = new Plane(bottom, Vector3d.ZAxis);
                Plane secondPlane = new Plane(top, Vector3d.ZAxis);
                Plane averagePlane = new Plane(mid, Vector3d.ZAxis);

                packedOffcuts.Add(Offcut.CreateOffcut(
                    closedOffcut,
                    source.Index,
                    ocX,
                    ocY,
                    ocZ,
                    source.Vol > 0 ? source.Vol : ocX * ocY * ocZ,
                    ocX * ocY * ocZ,
                    firstPlane,
                    secondPlane,
                    averagePlane,
                    averagePlane,
                    basePlane,
                    0));
            }

            return packedOffcuts;
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