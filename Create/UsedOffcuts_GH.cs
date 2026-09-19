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
    public class UsedOffcuts_GH : GH_Component
    {
        public UsedOffcuts_GH()
          : base("Used Offcuts", "UsedOc", "List which numbered scraps were used, and leftovers if full stock is given", "Spruce Beetle", "     Create")
        {
            ApplyDisplayNames();
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed or aligned Offcuts (used scraps)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Offcut Data", "OcD", "Full CSV stock list (optional; for unused scrap numbers)", GH_ParamAccess.list);

            pManager[1].Optional = true;

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Used", "U", "Scrap numbers that were used, sorted", GH_ParamAccess.list);
            pManager.AddNumberParameter("Unused", "Un", "Scrap numbers still in OcD that were not used, sorted. Empty if OcD is unwired.", GH_ParamAccess.list);

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
            Name = "Used Offcuts";
            NickName = "UsedOc";
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<SpruceBeetle.Offcut> usedOffcuts = new List<SpruceBeetle.Offcut>();
            List<SpruceBeetle.Offcut> stock = new List<SpruceBeetle.Offcut>();

            if (!DA.GetDataList(0, usedOffcuts)) return;
            bool hasStock = DA.GetDataList(1, stock);

            List<double> usedNumbers = new List<double>();
            HashSet<double> usedSet = new HashSet<double>();

            if (usedOffcuts == null || usedOffcuts.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The used Offcut list is empty!");
            }
            else
            {
                for (int i = 0; i < usedOffcuts.Count; i++)
                {
                    if (usedOffcuts[i] == null)
                        continue;

                    double index = usedOffcuts[i].Index;
                    usedNumbers.Add(index);
                    usedSet.Add(index);
                }
            }

            List<double> unusedNumbers = new List<double>();

            if (hasStock && stock != null && stock.Count > 0)
            {
                HashSet<double> stockSet = new HashSet<double>();

                for (int i = 0; i < stock.Count; i++)
                {
                    if (stock[i] == null)
                        continue;

                    double index = stock[i].Index;
                    stockSet.Add(index);

                    if (!usedSet.Contains(index))
                        unusedNumbers.Add(index);
                }

                List<double> missing = new List<double>();
                for (int i = 0; i < usedNumbers.Count; i++)
                {
                    double index = usedNumbers[i];
                    if (!stockSet.Contains(index) && !missing.Contains(index))
                        missing.Add(index);
                }

                if (missing.Count > 0)
                {
                    missing.Sort();
                    string labels = string.Join(", ", missing.ConvertAll(v => v.ToString()));
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Used scrap number not in stock: " + labels);
                }
            }

            usedNumbers.Sort();
            unusedNumbers.Sort();

            DA.SetDataList(0, usedNumbers);
            DA.SetDataList(1, unusedNumbers);
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_UpdateList;

        // component giud
        public override Guid ComponentGuid => new Guid("C8E41A27-6D3F-4B90-9F15-2A7E5C04B8D1");
    }
}
