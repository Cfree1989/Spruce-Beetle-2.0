using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class SelectContacts_GH : GH_Component
    {
        IGH_Param parameter = null;

        public SelectContacts_GH()
          : base("Select Contacts", "PickContacts",
              "Keep a subset of packed face contacts (all, Z beds, or XY stitches)",
              "Spruce Beetle", "   Packing")
        {
            ApplyDisplayNames();
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Contacts", "C", "Contacts from Packed Contacts", GH_ParamAccess.list);
            pManager.AddTextParameter("Mode", "M", "All, Z, or XY", GH_ParamAccess.item);
            parameter = pManager[1];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Contacts", "C", "Selected contacts", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Planes", "P", "Planes of selected contacts. Preview only; baking this component skips planes", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
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
            Name = "Select Contacts";
            NickName = "PickContacts";
        }


        protected override void BeforeSolveInstance()
        {
            GH_ValueList list = null;
            foreach (var source in parameter.Sources)
            {
                if (source is GH_ValueList vl)
                {
                    list = vl;
                    break;
                }
            }

            if (list == null)
            {
                if (Instances.ActiveCanvas?.Document == null)
                    return;

                list = new GH_ValueList();
                list.CreateAttributes();
                list.Attributes.Pivot = new System.Drawing.PointF(Attributes.Pivot.X - 200, Attributes.Pivot.Y);
                Instances.ActiveCanvas.Document.AddObject(list, false);
                parameter.AddSource(list);
            }

            SyncModeList(list);
            parameter.CollectData();
        }


        private static void SyncModeList(GH_ValueList list)
        {
            List<string> wanted = PackedNeighbors.ContactModes();
            if (list.ListItems.Count == wanted.Count)
            {
                bool same = true;
                for (int i = 0; i < wanted.Count; i++)
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


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var objs = new List<object>();
            string mode = "All";

            if (!DA.GetDataList(0, objs))
                return;
            if (!DA.GetData(1, ref mode))
                return;

            var contacts = new List<PackedContact>();
            for (int i = 0; i < objs.Count; i++)
            {
                PackedContact c = PackedContact_GH.Parse(objs[i]);
                if (c == null)
                    continue;
                contacts.Add(c);
            }

            if (contacts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No contacts provided.");
                return;
            }

            List<PackedContact> selected;
            string key = (mode ?? "All").Trim();
            if (string.Equals(key, "Z", StringComparison.OrdinalIgnoreCase))
                selected = contacts.FindAll(c => c.Axis == ContactAxis.Z);
            else if (string.Equals(key, "XY", StringComparison.OrdinalIgnoreCase))
                selected = contacts.FindAll(c => c.Axis != ContactAxis.Z);
            else
            {
                selected = contacts;
                if (string.Equals(key, "Seams", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "Connected", StringComparison.OrdinalIgnoreCase))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"{key} was removed with T-junction logic; using All.");
                    key = "All";
                }
                else if (!string.Equals(key, "All", StringComparison.OrdinalIgnoreCase) && key.Length > 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Unknown mode '{key}'; using All.");
                    key = "All";
                }
                else
                    key = "All";
            }

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
                if (param is Param_Plane)
                    continue;
                if (param is IGH_BakeAwareObject baker)
                    baker.BakeGeometry(doc, att, obj_ids);
            }
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_Unification;

        public override Guid ComponentGuid => new Guid("1D9A6E40-C3B2-4F58-A817-6E0C4D92F1AB");
    }
}
