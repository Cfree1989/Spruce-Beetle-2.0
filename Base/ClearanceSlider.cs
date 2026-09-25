using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;


namespace SpruceBeetle
{
    /// <summary>
    /// Auto-wires a clearance slider limited to a few thousandths of a model unit.
    /// </summary>
    public static class ClearanceSlider
    {
        public const double Default = 0.005;
        public const string InitCode = "0.001<0.005<0.01";

        public static void Ensure(IGH_Param param, GH_Component owner)
        {
            if (param == null || owner?.Attributes == null)
                return;
            if (param.SourceCount > 0)
                return;
            if (Instances.ActiveCanvas?.Document == null)
                return;

            var slider = new GH_NumberSlider();
            slider.CreateAttributes();
            slider.SetInitCode(InitCode);

            PointF pivot = param.Attributes != null ? param.Attributes.Pivot : owner.Attributes.Pivot;
            slider.Attributes.Pivot = new PointF(pivot.X - 180, pivot.Y);
            Instances.ActiveCanvas.Document.AddObject(slider, false);
            param.AddSource(slider);
            param.CollectData();
        }


        public static double Read(GH_Component owner, double value)
        {
            if (value < 0)
            {
                owner.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Clearance raised to 0.");
                return 0;
            }
            return value;
        }
    }
}
