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
using GH_IO.Serialization;
using Grasshopper.Kernel;


namespace SpruceBeetle.Create
{
    public class RemainingOffcuts_GH : GH_Component
    {
        public RemainingOffcuts_GH()
          : base("Remaining Offcuts", "Remain",
              "Drop scraps that were already placed. Match by scrap number.",
              "Spruce Beetle", "     Create")
        {
        }


        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Pile", "P", "Every scrap still on hand. Merge the column CSV lists into this pin. The pin is flattened.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Used", "U", "Scraps already placed. Merge each packed column Oc into this pin. The pin is flattened.", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
            {
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
                pManager[i].DataMapping = GH_DataMapping.Flatten;
            }
        }


        public override bool Read(GH_IReader reader)
        {
            bool ok = base.Read(reader);
            FlattenInputs();
            return ok;
        }


        public override void AddedToDocument(GH_Document document)
        {
            FlattenInputs();
            base.AddedToDocument(document);
        }


        void FlattenInputs()
        {
            if (Params == null)
                return;

            for (int i = 0; i < Params.Input.Count; i++)
                Params.Input[i].DataMapping = GH_DataMapping.Flatten;
        }


        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Remaining", "R", "Pile with the used scrap numbers removed. Feed the next column.", GH_ParamAccess.list);

            pManager[0].WireDisplay = GH_ParamWireDisplay.faint;
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Offcut> pile = new List<Offcut>();
            List<Offcut> used = new List<Offcut>();

            if (!DA.GetDataList(0, pile)) return;
            if (!DA.GetDataList(1, used)) return;

            HashSet<double> usedIds = new HashSet<double>();
            for (int i = 0; i < used.Count; i++)
            {
                if (used[i] == null)
                    continue;
                usedIds.Add(used[i].Index);
            }

            HashSet<double> pileIds = new HashSet<double>();
            List<Offcut_GH> remaining = new List<Offcut_GH>();

            for (int i = 0; i < pile.Count; i++)
            {
                if (pile[i] == null)
                    continue;

                double index = pile[i].Index;
                pileIds.Add(index);

                if (!usedIds.Contains(index))
                    remaining.Add(new Offcut_GH(pile[i]));
            }

            List<double> missing = new List<double>();
            foreach (double index in usedIds)
            {
                if (!pileIds.Contains(index))
                    missing.Add(index);
            }

            if (missing.Count > 0)
            {
                missing.Sort();
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Used scrap number not in the pile: " + string.Join(", ", missing));
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                remaining.Count + " of " + pile.Count + " offcut(s) remain. " + usedIds.Count + " scrap number(s) removed.");

            DA.SetDataList(0, remaining);
        }


        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_UpdateList;

        public override Guid ComponentGuid => new Guid("A4C7E2B1-9D58-4F63-8C0A-6B1E47D29F35");
    }
}
