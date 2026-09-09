using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class PackedContacts_GH : GH_Component
    {
        public PackedContacts_GH()
          : base("Packed Contacts", "PackContacts",
              "Find face-to-face contacts between packed Offcuts (Z beds and XY stitches)",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Packed Offcuts", "Oc", "Offcuts from Bin Packing EB-AFIT", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tolerance", "T", "Maximum gap to treat as a face contact", GH_ParamAccess.item, 0.01);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Contacts", "C", "Face contacts between packed Offcuts", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Planes", "P", "Contact planes at overlap centers", GH_ParamAccess.list);
            pManager.AddCurveParameter("Rectangles", "R", "Overlap rectangles of each contact", GH_ParamAccess.list);
            pManager.AddTextParameter("Axis", "A", "Contact axis: Z, X, or Y", GH_ParamAccess.list);

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

            List<PackedContact> contacts = PackedNeighbors.AllContacts(packed, tolerance);
            var goos = new List<PackedContact_GH>(contacts.Count);
            var planes = new List<Plane>(contacts.Count);
            var rects = new List<Curve>(contacts.Count);
            var axes = new List<string>(contacts.Count);
            int zCount = 0;
            int xyCount = 0;

            for (int i = 0; i < contacts.Count; i++)
            {
                PackedContact c = contacts[i];
                goos.Add(new PackedContact_GH(c));
                planes.Add(c.Plane);
                Curve rect = c.OverlapRectangle();
                if (rect != null)
                    rects.Add(rect);
                axes.Add(c.AxisName);
                if (c.Axis == ContactAxis.Z)
                    zCount++;
                else
                    xyCount++;
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{contacts.Count} contact(s): {zCount} Z, {xyCount} XY.");

            DA.SetDataList(0, goos);
            DA.SetDataList(1, planes);
            DA.SetDataList(2, rects);
            DA.SetDataList(3, axes);
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_IntersectionJoints;

        public override Guid ComponentGuid => new Guid("B4C8E2A1-7F3D-4B19-9E6C-2A5D8F1B0473");
    }
}
