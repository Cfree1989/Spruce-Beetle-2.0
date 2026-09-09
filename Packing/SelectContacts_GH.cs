using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class SelectContacts_GH : GH_Component
    {
        GH_ValueList valueList = null;
        IGH_Param parameter = null;

        public SelectContacts_GH()
          : base("Select Contacts", "PickJoints",
              "Keep a subset of packed face contacts (all, Z, XY, seams, or connectivity)",
              "Spruce Beetle", "   Packing")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Contacts", "C", "Contacts from Packed Contacts", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Piece Count", "N", "Number of packed Offcuts (for Connected mode). Optional if Contacts cover all indices.", GH_ParamAccess.item);
            pManager.AddTextParameter("Mode", "M", "All, Z, XY, Seams, or Connected", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "T", "Gap used to detect seam corners", GH_ParamAccess.item, 0.01);

            pManager[1].Optional = true;
            parameter = pManager[2];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Contacts", "C", "Selected contacts", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Planes", "P", "Planes of selected contacts", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void BeforeSolveInstance()
        {
            if (valueList != null)
                return;

            if (parameter.Sources.Count == 0)
                valueList = new GH_ValueList();
            else
            {
                foreach (var source in parameter.Sources)
                {
                    if (source is GH_ValueList)
                        valueList = source as GH_ValueList;
                    return;
                }
            }

            if (Instances.ActiveCanvas?.Document == null)
                return;

            valueList.CreateAttributes();
            valueList.Attributes.Pivot = new System.Drawing.PointF(Attributes.Pivot.X - 200, Attributes.Pivot.Y);
            valueList.ListItems.Clear();

            foreach (string mode in PackedNeighbors.ContactModes())
                valueList.ListItems.Add(new GH_ValueListItem(mode, $"\"{mode}\""));

            Instances.ActiveCanvas.Document.AddObject(valueList, false);
            parameter.AddSource(valueList);
            parameter.CollectData();
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var objs = new List<object>();
            string mode = "All";
            double tolerance = 0.01;
            int pieceCount = 0;

            if (!DA.GetDataList(0, objs))
                return;
            DA.GetData(1, ref pieceCount);
            if (!DA.GetData(2, ref mode))
                return;
            DA.GetData(3, ref tolerance);

            var contacts = new List<PackedContact>();
            int maxIndex = -1;
            for (int i = 0; i < objs.Count; i++)
            {
                PackedContact c = PackedContact_GH.Parse(objs[i]);
                if (c == null)
                    continue;
                contacts.Add(c);
                maxIndex = Math.Max(maxIndex, Math.Max(c.IndexA, c.IndexB));
            }

            if (contacts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No contacts provided.");
                return;
            }

            if (pieceCount < 1)
                pieceCount = maxIndex + 1;

            if (tolerance < 0)
                tolerance = 0;

            List<PackedContact> selected;
            string key = (mode ?? "All").Trim();
            if (string.Equals(key, "Z", StringComparison.OrdinalIgnoreCase))
                selected = contacts.FindAll(c => c.Axis == ContactAxis.Z);
            else if (string.Equals(key, "XY", StringComparison.OrdinalIgnoreCase))
                selected = contacts.FindAll(c => c.Axis != ContactAxis.Z);
            else if (string.Equals(key, "Seams", StringComparison.OrdinalIgnoreCase))
                selected = PackedNeighbors.SelectSeams(contacts, tolerance);
            else if (string.Equals(key, "Connected", StringComparison.OrdinalIgnoreCase))
                selected = PackedNeighbors.SelectConnected(contacts, pieceCount, tolerance);
            else
                selected = contacts;

            var goos = new List<PackedContact_GH>(selected.Count);
            var planes = new List<Plane>(selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                goos.Add(new PackedContact_GH(selected[i]));
                planes.Add(selected[i].Plane);
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"{selected.Count} of {contacts.Count} contact(s) kept ({key}).");

            DA.SetDataList(0, goos);
            DA.SetDataList(1, planes);
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_Unification;

        public override Guid ComponentGuid => new Guid("1D9A6E40-C3B2-4F58-A817-6E0C4D92F1AB");
    }
}
