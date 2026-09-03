using System;
using System.Collections.Generic;
using Rhino.Geometry;


namespace SpruceBeetle.Packing
{
    internal static class PackedNeighbors
    {
        public static BoundingBox WorldBox(Offcut offcut)
        {
            if (offcut?.OffcutGeometry == null)
                return BoundingBox.Empty;

            return offcut.OffcutGeometry.GetBoundingBox(true);
        }


        public static bool ZContact(BoundingBox a, BoundingBox b, double tolerance, out Plane contact)
        {
            contact = Plane.Unset;

            if (!a.IsValid || !b.IsValid)
                return false;

            if (!Overlap(a.Min.X, a.Max.X, b.Min.X, b.Max.X, tolerance))
                return false;
            if (!Overlap(a.Min.Y, a.Max.Y, b.Min.Y, b.Max.Y, tolerance))
                return false;

            double z;
            if (Math.Abs(a.Max.Z - b.Min.Z) <= tolerance)
                z = a.Max.Z;
            else if (Math.Abs(b.Max.Z - a.Min.Z) <= tolerance)
                z = b.Max.Z;
            else
                return false;

            double x0 = Math.Max(a.Min.X, b.Min.X);
            double x1 = Math.Min(a.Max.X, b.Max.X);
            double y0 = Math.Max(a.Min.Y, b.Min.Y);
            double y1 = Math.Min(a.Max.Y, b.Max.Y);

            contact = new Plane(new Point3d((x0 + x1) * 0.5, (y0 + y1) * 0.5, z), Vector3d.ZAxis);
            return true;
        }


        public static List<List<int>> ZStacks(List<Offcut> packed, double tolerance, out List<Plane> contacts)
        {
            contacts = new List<Plane>();
            int count = packed.Count;
            var boxes = new BoundingBox[count];
            var parent = new int[count];

            for (int i = 0; i < count; i++)
            {
                boxes[i] = WorldBox(packed[i]);
                parent[i] = i;
            }

            int Find(int x)
            {
                if (parent[x] == x)
                    return x;
                parent[x] = Find(parent[x]);
                return parent[x];
            }

            void Union(int a, int b)
            {
                int pa = Find(a);
                int pb = Find(b);
                if (pa != pb)
                    parent[pa] = pb;
            }

            for (int i = 0; i < count; i++)
            {
                for (int j = i + 1; j < count; j++)
                {
                    if (!ZContact(boxes[i], boxes[j], tolerance, out Plane plane))
                        continue;

                    Union(i, j);
                    contacts.Add(plane);
                }
            }

            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < count; i++)
            {
                int root = Find(i);
                if (!groups.TryGetValue(root, out List<int> members))
                {
                    members = new List<int>();
                    groups[root] = members;
                }
                members.Add(i);
            }

            var stacks = new List<List<int>>();
            foreach (List<int> members in groups.Values)
            {
                members.Sort((a, b) => boxes[a].Min.Z.CompareTo(boxes[b].Min.Z));
                stacks.Add(members);
            }

            stacks.Sort((a, b) =>
            {
                int byZ = boxes[a[0]].Min.Z.CompareTo(boxes[b[0]].Min.Z);
                if (byZ != 0)
                    return byZ;
                return boxes[a[0]].Min.X.CompareTo(boxes[b[0]].Min.X);
            });

            return stacks;
        }


        private static bool Overlap(double a0, double a1, double b0, double b1, double tolerance)
        {
            return a0 < b1 - tolerance && b0 < a1 - tolerance;
        }
    }
}
