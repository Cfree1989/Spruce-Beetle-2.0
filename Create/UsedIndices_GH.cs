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


namespace SpruceBeetle.Create
{
    public class UsedIndices_GH : GH_Component
    {
        public UsedIndices_GH()
          : base("Used Offcuts", "UsedOc", "List the stock numbers of used Offcuts, and leftovers if full stock is given", "Spruce Beetle", "     Create")
        {
        }


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Packed or aligned Offcuts (used pieces)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Offcut Data", "OcD", "Full CSV stock list (optional; for unused numbers)", GH_ParamAccess.list);

            pManager[1].Optional = true;

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Used", "U", "Stock numbers of used Offcuts, sorted numerically", GH_ParamAccess.list);
            pManager.AddNumberParameter("Unused", "Un", "Stock numbers still in OcD that were not used, sorted numerically. Empty if OcD is unwired.", GH_ParamAccess.list);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<SpruceBeetle.Offcut> usedOffcuts = new List<SpruceBeetle.Offcut>();
            List<SpruceBeetle.Offcut> stock = new List<SpruceBeetle.Offcut>();

            if (!DA.GetDataList(0, usedOffcuts)) return;
            bool hasStock = DA.GetDataList(1, stock);

            List<double> usedIndices = new List<double>();
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
                    usedIndices.Add(index);
                    usedSet.Add(index);
                }
            }

            List<double> unusedIndices = new List<double>();

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
                        unusedIndices.Add(index);
                }

                List<double> missing = new List<double>();
                for (int i = 0; i < usedIndices.Count; i++)
                {
                    double index = usedIndices[i];
                    if (!stockSet.Contains(index) && !missing.Contains(index))
                        missing.Add(index);
                }

                if (missing.Count > 0)
                {
                    missing.Sort();
                    string labels = string.Join(", ", missing.ConvertAll(v => v.ToString()));
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Used Index not in stock: " + labels);
                }
            }

            usedIndices.Sort();
            unusedIndices.Sort();

            DA.SetDataList(0, usedIndices);
            DA.SetDataList(1, unusedIndices);
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
