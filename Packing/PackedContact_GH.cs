using System;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    public class PackedContact_GH : GH_Goo<PackedContact>
    {
        public PackedContact_GH() : this(null) { }

        public PackedContact_GH(PackedContact native)
        {
            Value = native;
        }

        public override IGH_Goo Duplicate()
        {
            if (Value == null)
                return new PackedContact_GH();
            return new PackedContact_GH(Value.Duplicate());
        }

        public static PackedContact Parse(object obj)
        {
            if (obj is PackedContact_GH goo)
                return goo.Value;
            return obj as PackedContact;
        }

        public override string ToString()
        {
            if (Value == null)
                return "Null PackedContact";
            return $"Contact {Value.IndexA}-{Value.IndexB} {Value.AxisName} A={Value.Area:0.###}";
        }

        public override string TypeName => "PackedContact";
        public override string TypeDescription => "Face contact between two packed Offcuts";
        public override object ScriptVariable() => Value;

        public override bool IsValid => Value != null && Value.Overlap.IsValid;

        public override string IsValidWhyNot
        {
            get
            {
                if (Value == null)
                    return "No data";
                if (!Value.Overlap.IsValid)
                    return "Invalid overlap";
                return string.Empty;
            }
        }

        public override bool CastFrom(object source)
        {
            if (source == null)
                return false;

            if (source is PackedContact contact)
            {
                Value = contact;
                return true;
            }

            if (source is PackedContact_GH goo)
            {
                Value = goo.Value;
                return true;
            }

            return false;
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (Value == null)
                return false;

            if (typeof(Q).IsAssignableFrom(typeof(GH_Plane)))
            {
                target = (Q)(object)new GH_Plane(Value.Plane);
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(Plane)))
            {
                target = (Q)(object)Value.Plane;
                return true;
            }

            if (typeof(Q).IsAssignableFrom(typeof(PackedContact)))
            {
                target = (Q)(object)Value;
                return true;
            }

            return false;
        }

        public override bool Write(GH_IWriter writer)
        {
            if (Value == null)
                return false;

            writer.SetInt32("a", Value.IndexA);
            writer.SetInt32("b", Value.IndexB);
            writer.SetInt32("axis", (int)Value.Axis);
            writer.SetDouble("area", Value.Area);
            Plane p = Value.Plane;
            writer.SetPlane("pl", new GH_IO.Types.GH_Plane(
                p.OriginX, p.OriginY, p.OriginZ,
                p.XAxis.X, p.XAxis.Y, p.XAxis.Z,
                p.YAxis.X, p.YAxis.Y, p.YAxis.Z));
            writer.SetPoint3D("omin", new GH_IO.Types.GH_Point3D(Value.Overlap.Min.X, Value.Overlap.Min.Y, Value.Overlap.Min.Z));
            writer.SetPoint3D("omax", new GH_IO.Types.GH_Point3D(Value.Overlap.Max.X, Value.Overlap.Max.Y, Value.Overlap.Max.Z));
            return true;
        }

        public override bool Read(GH_IReader reader)
        {
            int a = reader.GetInt32("a");
            int b = reader.GetInt32("b");
            var axis = (ContactAxis)reader.GetInt32("axis");
            double area = reader.GetDouble("area");
            GH_IO.Types.GH_Plane gp = reader.GetPlane("pl");
            Plane plane = new Plane(
                new Point3d(gp.Origin.x, gp.Origin.y, gp.Origin.z),
                new Vector3d(gp.XAxis.x, gp.XAxis.y, gp.XAxis.z),
                new Vector3d(gp.YAxis.x, gp.YAxis.y, gp.YAxis.z));
            GH_IO.Types.GH_Point3D mn = reader.GetPoint3D("omin");
            GH_IO.Types.GH_Point3D mx = reader.GetPoint3D("omax");
            var overlap = new BoundingBox(
                new Point3d(mn.x, mn.y, mn.z),
                new Point3d(mx.x, mx.y, mx.z));

            Value = new PackedContact
            {
                IndexA = a,
                IndexB = b,
                Axis = axis,
                Plane = plane,
                Overlap = overlap,
                Area = area
            };
            return true;
        }
    }
}
