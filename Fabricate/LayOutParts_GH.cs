using System;
using System.Collections.Generic;
using System.Globalization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;


namespace SpruceBeetle.Fabricate
{
    public class LayOutParts_GH : GH_Component
    {
        IGH_Param layoutParameter = null;
        readonly List<Brep> bakedSolids = new List<Brep>();
        readonly List<Plane> bakedFrames = new List<Plane>();
        readonly List<string> bakedNames = new List<string>();

        public LayOutParts_GH()
          : base("Lay Out Parts", "LayOut",
              "Squares each part to a base plane and places them in a spaced row or stacked at one origin, for a Fusion import",
              "Spruce Beetle", "    Fabricate")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Parts", "G", "Offcuts or breps to lay out. Packed or aligned pieces both work", GH_ParamAccess.list);
            pManager.AddTextParameter("Layout", "L", "Line spaces parts along the base X axis, lowest scrap number first. Point stacks every part on the base origin", GH_ParamAccess.item, "Line");
            pManager.AddNumberParameter("Spacing", "S", "Clear gap between parts along the row. Leave empty to use the thickest part's smallest side", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Base", "BP", "Plane the parts sit on. Length runs along Y, the row along X, thinnest edge along Z", GH_ParamAccess.item, Plane.WorldXY);
            pManager.AddNumberParameter("Scrap Numbers", "N", "Optional. Scrap numbers in the same order as Parts. Leave empty when Parts are Offcuts; their own numbers are used. Do not wire the sorted Used Offcuts list", GH_ParamAccess.list);
            pManager[2].Optional = true;
            pManager[4].Optional = true;
            layoutParameter = pManager[1];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Parts", "G", "Solids squared to the base plane. Bake the component: each scrap becomes a block named with its number, which Fusion opens as a component", GH_ParamAccess.list);
            pManager.AddGenericParameter("Offcuts", "Oc", "Same parts as Offcuts, in the new pose", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Frames", "F", "Corner frame of each part. Longest edge on Y, thinnest on Z", GH_ParamAccess.list);
            pManager.HideParameter(2);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void BeforeSolveInstance()
        {
            bakedSolids.Clear();
            bakedFrames.Clear();
            bakedNames.Clear();

            if (layoutParameter == null)
                return;

            GH_ValueList list = null;
            foreach (var source in layoutParameter.Sources)
            {
                if (source is GH_ValueList valueList)
                {
                    list = valueList;
                    break;
                }
            }

            if (list == null)
            {
                if (layoutParameter.Sources.Count > 0)
                    return;
                if (Instances.ActiveCanvas?.Document == null)
                    return;

                list = new GH_ValueList();
                list.CreateAttributes();
                list.Attributes.Pivot = new System.Drawing.PointF(Attributes.Pivot.X - 200, Attributes.Pivot.Y + 22);
                Instances.ActiveCanvas.Document.AddObject(list, false);
                layoutParameter.AddSource(list);
            }

            SyncLayoutList(list);
            layoutParameter.CollectData();
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var raw = new List<object>();
            string layout = "Line";
            double spacing = 0;
            Plane basis = Plane.WorldXY;
            var numbers = new List<double>();

            if (!DA.GetDataList(0, raw))
                return;
            DA.GetData(1, ref layout);
            bool hasSpacing = DA.GetData(2, ref spacing);
            DA.GetData(3, ref basis);
            bool hasNumbers = DA.GetDataList(4, numbers);

            if (raw.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No parts provided.");
                return;
            }

            if (!TryResolveLayout(layout, out bool line))
                return;

            if (hasSpacing && spacing < 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Spacing was below 0 and was set to 0.");
                spacing = 0;
            }

            double tolerance = RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.001;
            var pieces = new List<PartLayout.Piece>();
            int skipped = 0;
            int worldBoxed = 0;

            for (int i = 0; i < raw.Count; i++)
            {
                if (!TryReadPart(raw[i], out Offcut source, out Brep brep))
                {
                    skipped++;
                    continue;
                }

                bool hasStock = source != null && source.BasePlane.IsValid;
                Plane stock = hasStock ? source.BasePlane : Plane.Unset;
                if (!PartLayout.TryMeasure(brep, stock, hasStock, tolerance, out PartLayout.Piece piece))
                {
                    skipped++;
                    continue;
                }

                piece.Source = source;
                piece.InputOrder = i;
                piece.HasNumber = source != null;
                piece.ScrapNumber = source != null ? source.Index : 0;
                if (piece.UsedWorldBox)
                    worldBoxed++;
                pieces.Add(piece);
            }

            if (pieces.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "None of the parts had geometry that could be laid out.");
                return;
            }

            if (hasNumbers)
            {
                if (numbers.Count != raw.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"Scrap Numbers has {numbers.Count} values and Parts has {raw.Count}. Names follow the shorter list.");
                }

                for (int p = 0; p < pieces.Count; p++)
                {
                    int slot = pieces[p].InputOrder;
                    if (slot < 0 || slot >= numbers.Count)
                        continue;
                    pieces[p].ScrapNumber = numbers[slot];
                    pieces[p].HasNumber = true;
                }
            }

            double gap = 0;
            if (line)
            {
                PartLayout.SortByNumber(pieces);
                gap = hasSpacing ? spacing : PartLayout.AutomaticGap(pieces);
            }

            var breps = new List<Brep>();
            var offcuts = new List<Offcut_GH>();
            var frames = new List<Plane>();
            var placedOffcuts = new List<Offcut>();
            var placedPieces = new List<PartLayout.Piece>();
            PartLayout.Place(pieces, basis, line, gap, breps, placedOffcuts, frames, placedPieces);
            for (int i = 0; i < placedOffcuts.Count; i++)
                offcuts.Add(new Offcut_GH(placedOffcuts[i]));

            int missingNumbers = 0;
            for (int i = 0; i < breps.Count; i++)
            {
                bakedSolids.Add(breps[i]);
                bakedFrames.Add(i < frames.Count ? frames[i] : Plane.WorldXY);
                PartLayout.Piece placed = i < placedPieces.Count ? placedPieces[i] : null;
                if (placed != null && placed.HasNumber)
                    bakedNames.Add(FormatScrapNumber(placed.ScrapNumber));
                else
                {
                    missingNumbers++;
                    bakedNames.Add("part-" + (i + 1).ToString(CultureInfo.InvariantCulture));
                }
            }

            if (missingNumbers > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{missingNumbers} part(s) had no scrap number, so their blocks are named part-1, part-2, and so on. Wire the Offcuts into Parts, not only the breps.");
            }

            if (skipped > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Skipped {skipped} part(s) with no usable geometry.");
            }

            if (worldBoxed > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"{worldBoxed} part(s) had no square faces, so they were placed by their world box and may still be tilted.");
            }

            string gapText = gap.ToString("0.###", CultureInfo.InvariantCulture);
            if (line)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"{breps.Count} parts in a line, lowest scrap number first, gap {gapText}. Longest edge along base Y, thinnest edge along base Z.");
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"{breps.Count} parts stacked on the base origin. Longest edge along base Y, thinnest edge along base Z.");
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Bake this component for one Rhino block per scrap, named with its number. Fusion opens those blocks as components.");

            DA.SetDataList(0, breps);
            DA.SetDataList(1, offcuts);
            DA.SetDataList(2, frames);
        }


        static bool TryReadPart(object raw, out Offcut offcut, out Brep brep)
        {
            offcut = null;
            brep = null;
            if (raw is GH_ObjectWrapper wrapper)
                raw = wrapper.Value;
            if (raw == null)
                return false;

            if (raw is Offcut_GH offcutGoo)
                offcut = offcutGoo.Value;
            else if (raw is Offcut directOffcut)
                offcut = directOffcut;
            else if (raw is IGH_Goo goo && goo.CastTo(out Offcut castOffcut))
                offcut = castOffcut;

            if (offcut?.OffcutGeometry != null)
            {
                brep = offcut.OffcutGeometry;
                return true;
            }

            if (raw is GH_Brep ghBrep && ghBrep.Value != null)
            {
                brep = ghBrep.Value;
                return true;
            }

            if (raw is Brep directBrep)
            {
                brep = directBrep;
                return true;
            }

            if (raw is IGH_Goo geometry && geometry.CastTo(out Brep castBrep) && castBrep != null)
            {
                brep = castBrep;
                return true;
            }

            return false;
        }


        bool TryResolveLayout(string layout, out bool line)
        {
            string key = (layout ?? string.Empty).Trim();
            if (key.Length >= 2 && key[0] == '"' && key[key.Length - 1] == '"')
                key = key.Substring(1, key.Length - 2).Trim();

            if (key.Equals("Point", StringComparison.OrdinalIgnoreCase))
            {
                line = false;
                return true;
            }

            line = true;
            if (key.Length == 0 || key.Equals("Line", StringComparison.OrdinalIgnoreCase))
                return true;

            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                $"Unknown layout '{key}'; using Line.");
            return true;
        }


        static void SyncLayoutList(GH_ValueList list)
        {
            string[] wanted = { "Line", "Point" };
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


        public override void BakeGeometry(RhinoDoc doc, List<Guid> obj_ids)
        {
            BakeGeometry(doc, doc?.CreateDefaultAttributes(), obj_ids);
        }


        public override void BakeGeometry(RhinoDoc doc, ObjectAttributes att, List<Guid> obj_ids)
        {
            if (doc == null)
                return;
            if (bakedSolids.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Nothing to bake.");
                return;
            }

            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            uint undo = doc.BeginUndoRecord("Lay Out Parts");
            try
            {
                for (int i = 0; i < bakedSolids.Count; i++)
                {
                    Brep solid = bakedSolids[i];
                    if (solid == null)
                        continue;

                    string number = i < bakedNames.Count ? bakedNames[i] : (i + 1).ToString(CultureInfo.InvariantCulture);
                    string name = NextBlockName(doc, number, used);
                    Plane frame = i < bakedFrames.Count && bakedFrames[i].IsValid ? bakedFrames[i] : Plane.WorldXY;

                    Brep local = solid.DuplicateBrep();
                    if (local == null)
                        continue;
                    if (!local.Transform(Transform.PlaneToPlane(frame, Plane.WorldXY)))
                    {
                        local.Dispose();
                        BakeNamedBrep(doc, solid, name, att, obj_ids);
                        continue;
                    }

                    ObjectAttributes inner = att?.Duplicate() ?? doc.CreateDefaultAttributes();
                    inner.Name = name;
                    inner.SetUserString("scrap", number);

                    int definition = doc.InstanceDefinitions.Add(
                        name,
                        "Scrap " + number,
                        Point3d.Origin,
                        new GeometryBase[] { local },
                        new ObjectAttributes[] { inner });
                    if (definition < 0)
                    {
                        local.Dispose();
                        BakeNamedBrep(doc, solid, name, att, obj_ids);
                        continue;
                    }

                    ObjectAttributes instanceAttributes = att?.Duplicate() ?? doc.CreateDefaultAttributes();
                    instanceAttributes.Name = name;
                    instanceAttributes.SetUserString("scrap", number);
                    Guid id = doc.Objects.AddInstanceObject(definition, Transform.PlaneToPlane(Plane.WorldXY, frame), instanceAttributes);
                    if (id != Guid.Empty)
                        obj_ids?.Add(id);
                }
            }
            finally
            {
                doc.EndUndoRecord(undo);
            }
        }


        static void BakeNamedBrep(RhinoDoc doc, Brep solid, string name, ObjectAttributes att, List<Guid> obj_ids)
        {
            ObjectAttributes named = att?.Duplicate() ?? doc.CreateDefaultAttributes();
            named.Name = name;
            named.SetUserString("scrap", name);
            Guid id = doc.Objects.AddBrep(solid, named);
            if (id != Guid.Empty)
                obj_ids?.Add(id);
        }


        static string FormatScrapNumber(double index)
        {
            if (double.IsNaN(index) || double.IsInfinity(index))
                return "0";
            double rounded = Math.Round(index);
            if (Math.Abs(index - rounded) < 1e-6)
                return rounded.ToString("0", CultureInfo.InvariantCulture);
            return index.ToString("0.###", CultureInfo.InvariantCulture);
        }


        static string NextBlockName(RhinoDoc doc, string number, HashSet<string> used)
        {
            string candidate = number;
            int suffix = 2;
            while (used.Contains(candidate) || doc.InstanceDefinitions.Find(candidate) != null)
            {
                candidate = number + "-" + suffix;
                suffix++;
            }

            used.Add(candidate);
            return candidate;
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_MoveToOrigin;

        public override Guid ComponentGuid => new Guid("6F2A9C14-8E37-4B5D-A1C0-7D93E4B58F26");
    }
}
