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
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Rhino.Geometry;


namespace SpruceBeetle.Alignment
{
    public class TenonJoints_GH : GH_Component
    {
        public TenonJoints_GH()
          : base("Tenon Joints", "Tenon", "Create tenon joints between the aligned Offcuts", "Spruce Beetle", "    Alignment")
        {
        }


        // value list
        GH_ValueList valueList = null;
        IGH_Param parameter = null;
        IGH_Param clearanceParameter = null;


        // parameter inputs
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Aligned Offcuts", "AOc", "List of aligned Offcuts", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tool Diameter", "D", "CNC bit diameter. Sets the smallest tenon and the smallest corner the bit can cut", GH_ParamAccess.item, 0.01);
            pManager.AddNumberParameter("Joint X", "JX", "Joint dimension in X direction (raised to D if smaller)", GH_ParamAccess.item, 0.02);
            pManager.AddNumberParameter("Joint Y", "JY", "Joint dimension in Y direction (raised to D if smaller)", GH_ParamAccess.item, 0.05);
            pManager.AddNumberParameter("Joint Z", "JZ", "Joint dimension in Z direction (tenon depth through the interface)", GH_ParamAccess.item, 0.04);
            pManager.AddTextParameter("Joint Type", "JT", "Adds the specified joint type", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Tenon Count", "TC", "The number of tenons to be created", GH_ParamAccess.item, 1);

            pManager.AddCurveParameter("Custom Shape", "CS", "Creates a custom tenon from the specifc shape of a closed planar curve", GH_ParamAccess.item);
            pManager[7].Optional = true;
            pManager.AddNumberParameter("Tool Radius", "R", "Corner fillet radius (raised to D / 2 if smaller). Unwired uses D / 2", GH_ParamAccess.item);
            pManager[8].Optional = true;
            pManager.AddNumberParameter("Clearance", "Cl", "Gap on each side between the key and the pocket. Auto slider runs from 0.001 to 0.01", GH_ParamAccess.item, ClearanceSlider.Default);

            parameter = pManager[5];
            clearanceParameter = pManager[9];

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        // parameter outputs
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Offcuts", "Oc", "Offcuts after joint intersection", GH_ParamAccess.list);
            pManager.AddBrepParameter("Joints", "J", "The cutting and joining geometry", GH_ParamAccess.list);
            pManager.AddNumberParameter("Joint Volume", "JV", "Volume of the joints", GH_ParamAccess.list);

            pManager.HideParameter(1);

            for (int i = 0; i < pManager.ParamCount; i++)
                pManager[i].WireDisplay = GH_ParamWireDisplay.faint;
        }


        public override bool Read(GH_IReader reader)
        {
            bool ok = base.Read(reader);
            ApplyPinNames();
            return ok;
        }


        public override void AddedToDocument(GH_Document document)
        {
            ApplyPinNames();
            base.AddedToDocument(document);
        }


        void ApplyPinNames()
        {
            if (Params.Input.Count < 2)
                return;
            Params.Input[1].Name = "Tool Diameter";
            Params.Input[1].NickName = "D";
            if (Params.Input.Count > 8)
            {
                Params.Input[8].Name = "Tool Radius";
                Params.Input[8].NickName = "R";
            }
        }


        // create value list
        protected override void BeforeSolveInstance()
        {
            ClearanceSlider.Ensure(clearanceParameter, this);

            if (valueList == null)
            {
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

                valueList.CreateAttributes();
                valueList.Attributes.Pivot = new System.Drawing.PointF(this.Attributes.Pivot.X - 200, this.Attributes.Pivot.Y - 0);
                valueList.ListItems.Clear();

                List<string> jointTypes = Joint.TenonTypes();

                foreach (string param in jointTypes)
                    valueList.ListItems.Add(new GH_ValueListItem(param, $"\"{param}\""));

                Instances.ActiveCanvas.Document.AddObject(valueList, false);
                parameter.AddSource(valueList);
                parameter.CollectData();
            }
        }


        // main
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // variables to reference the input parameters to
            List<Offcut> alignedOffcuts = new List<Offcut>();
            double diameter = 0.01;
            double jointX = 0.0;
            double jointY = 0.0;
            double jointZ = 0.0;
            string jointKey = "";
            int tenonCount = 1;
            Curve jointShape = null;
            double toolRadius = 0;
            double clearance = ClearanceSlider.Default;

            // access input parameters
            if (!DA.GetDataList(0, alignedOffcuts)) return;
            if (!DA.GetData(1, ref diameter)) return;
            if (!DA.GetData(2, ref jointX)) return;
            if (!DA.GetData(3, ref jointY)) return;
            if (!DA.GetData(4, ref jointZ)) return;
            if (!DA.GetData(5, ref jointKey)) return;
            if (!DA.GetData(6, ref tenonCount)) return;
            DA.GetData(7, ref jointShape);
            bool hasRadius = DA.GetData(8, ref toolRadius);
            DA.GetData(9, ref clearance);
            clearance = ClearanceSlider.Read(this, clearance);

            if (diameter <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tool diameter must be greater than 0.");
                return;
            }

            if (jointX < diameter || jointY < diameter)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"JX / JY raised to the tool diameter ({diameter}).");
                jointX = Math.Max(jointX, diameter);
                jointY = Math.Max(jointY, diameter);
            }

            double minRadius = diameter * 0.5;
            if (!hasRadius || toolRadius < minRadius)
                toolRadius = minRadius;

            double maxFillet = Math.Min(jointX, jointY) * 0.5 - 1e-4;
            if (maxFillet > 1e-6)
                toolRadius = Math.Min(toolRadius, maxFillet);
            else
                toolRadius = 0;

            // check if the curve is closed and planar
            if (jointShape != null)
            {
                if (!jointShape.IsClosed)
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve is not closed!");
                else if (!jointShape.IsPlanar())
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The curve is not planar!");
            }

            // get joint type
            Dictionary<string, int> jointDict = Joint.GetJointType();
            int jointType = jointDict[jointKey];

            // initialise lists to store all the data
            Brep[] outputOffcuts = new Brep[alignedOffcuts.Count];
            Brep[,] outputJoints = new Brep[alignedOffcuts.Count, tenonCount];
            double[] outputOffcutVol = new double[alignedOffcuts.Count];
            double[] outputJointVol = new double[alignedOffcuts.Count];

            // parallel computed joints with boolean difference
            System.Threading.Tasks.Parallel.For(0, alignedOffcuts.Count, (i, state) =>
            {
                void Pair(Plane plane, double[] minValue, int positionIndex, out Brep[] keys, out Brep[] pockets)
                {
                    CreateTenons(plane, jointX, jointY, jointZ, toolRadius, minValue, positionIndex, jointType, tenonCount, jointShape, out keys);
                    if (clearance <= 1e-9)
                    {
                        pockets = keys;
                        return;
                    }

                    CreateTenons(plane, jointX + 2.0 * clearance, jointY + 2.0 * clearance, jointZ + 2.0 * clearance, toolRadius, minValue, positionIndex, jointType, tenonCount, jointShape, out pockets);
                }

                // get the base for the joint position of each Offcut
                List<double[]> minimumDimensions = Utility.GetMinimumDimension(alignedOffcuts, i);
                double[] firstMin = minimumDimensions[0];
                double[] secondMin = minimumDimensions[1];

                // create joints with their respective volumes according to the joint type
                if (i == 0)
                {
                    Pair(alignedOffcuts[i].SecondPlane, secondMin, alignedOffcuts[i].PositionIndex, out Brep[] keys, out Brep[] pockets);

                    Brep cutOffcut = Joint.CutOffcut(pockets, alignedOffcuts[i].OffcutGeometry);

                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);

                    outputJointVol[i] = keys[0].GetVolume(0.0001, 0.0001) * tenonCount;

                    for (int j = 0; j < keys.Length; j++)
                    {
                        outputJoints[i, j] = keys[j];
                    }
                }

                else if (i == alignedOffcuts.Count - 1)
                {
                    Pair(alignedOffcuts[i].FirstPlane, firstMin, alignedOffcuts[i].PositionIndex, out _, out Brep[] pockets);

                    Brep cutOffcut = Joint.CutOffcut(pockets, alignedOffcuts[i].OffcutGeometry);

                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }

                else
                {
                    Pair(alignedOffcuts[i].FirstPlane, firstMin, alignedOffcuts[i].PositionIndex, out _, out Brep[] firstPockets);
                    Pair(alignedOffcuts[i].SecondPlane, secondMin, alignedOffcuts[i].PositionIndex, out Brep[] secondKeys, out Brep[] secondPockets);

                    Brep[] cutterBreps = new Brep[tenonCount * 2];

                    outputJointVol[i] = secondKeys[0].GetVolume(0.0001, 0.0001) * tenonCount;

                    for (int j = 0; j < secondKeys.Length; j++)
                    {
                        cutterBreps[j] = firstPockets[j];
                        cutterBreps[j + tenonCount] = secondPockets[j];
                        outputJoints[i, j] = secondKeys[j];
                    }

                    Brep cutOffcut = Joint.CutOffcut(cutterBreps, alignedOffcuts[i].OffcutGeometry);

                    outputOffcuts[i] = cutOffcut;
                    outputOffcutVol[i] = cutOffcut.GetVolume(0.0001, 0.0001);
                }

                // stop parallel loop
                if (i >= alignedOffcuts.Count)
                {
                    state.Stop();
                    return;
                }

                if (state.IsStopped)
                    return;
            });

            // output data to new Offcut_GH list
            List<Offcut_GH> offcutGHList = new List<Offcut_GH>();

            for (int i = 0; i < alignedOffcuts.Count; i++)
            {
                Offcut localOffcut = new Offcut(alignedOffcuts[i])
                {
                    OffcutGeometry = outputOffcuts[i],
                    FabVol = outputOffcutVol[i]
                };

                Offcut_GH offcutGH = new Offcut_GH(localOffcut);
                offcutGHList.Add(offcutGH);
            }

            // access output parameters
            DA.SetDataList(0, offcutGHList);
            DA.SetDataList(1, outputJoints);
            DA.SetDataList(2, outputJointVol);
        }


        //------------------------------------------------------------
        // CreateTenons method
        //------------------------------------------------------------
        protected void CreateTenons(Plane plane, double jointX, double jointY, double jointZ, double toolRadius, double[] minValue, int positionIndex, int jointType, int tenonCount, Curve jointShape, out Brep[] returnJoints)
        {
            // initialise new plane
            Plane basePlane = new Plane(plane);

            // setting new origin point for the planes
            Rectangle3d originRect = Offcut.GetOffcutBase(minValue[0], minValue[1], basePlane, positionIndex);

            // change origin of planes
            basePlane.Origin = originRect.Center;

            // create joints according to the joint type
            switch (jointType)
            {
                case 0:
                    {
                        RectJoint(basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;

                case 1:
                    {
                        CrossJoint(basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;

                case 2:
                    {
                        CreateCustomTenon(jointShape, basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;

                default:
                    {
                        RectJoint(basePlane, jointX, jointY, jointZ, minValue, toolRadius, tenonCount, out returnJoints);
                    }
                    break;
            }
        }


        //------------------------------------------------------------
        // RectJoint method
        //------------------------------------------------------------
        protected void RectJoint(Plane plane, double jointX, double jointY, double jointZ, double[] minValue, double toolRadius, int tenonCount, out Brep[] joints)
        {
            // initialise empty brep variable
            joints = new Brep[tenonCount];

            // joint dimensions
            Interval dX = new Interval(-jointX / 2, jointX / 2);
            Interval dY = new Interval(-jointY / 2, jointY / 2);
            double dZ = jointZ;

            // assign plane
            Plane basePlane = plane;

            // move basePlane for centered extrusion
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * dZ / 2));

            // base points for the tenons
            Point3d[] divPts = Joint.GetPoints(basePlane, minValue[0], tenonCount);

            // create the tenon joints
            for (int i = 0; i < divPts.Length; i++)
            {
                // set new base for origin
                basePlane.Origin = divPts[i];

                // create first and second rectangle
                Rectangle3d baseRect = new Rectangle3d(basePlane, dY, dX);
                Curve baseCurve = Curve.CreateFilletCornersCurve(baseRect.ToNurbsCurve(), toolRadius, 0.0001, 0.0001);

                // extrude first base to create first joint
                Brep joint = Extrusion.Create(baseCurve, dZ, true).ToBrep();

                // clean-up
                joint.Faces.SplitKinkyFaces(0.0001);
                if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                    joint.Flip();

                joints[i] = joint;
            }
        }


        //------------------------------------------------------------
        // CrossJoint method
        //------------------------------------------------------------
        protected void CrossJoint(Plane plane, double jointX, double jointY, double jointZ, double[] minValue, double toolRadius, int tenonCount, out Brep[] joints)
        {
            // initialise empty brep variable
            joints = new Brep[tenonCount];

            // joint dimensions
            Interval dX = new Interval(-jointX / 2, jointX / 2);
            Interval dY1 = new Interval(-jointY / 2, jointY / 2);
            Interval dY2 = new Interval(-jointY / 3, jointY / 3);
            double dZ = jointZ;

            // assign plane
            Plane basePlane = new Plane(plane);

            // move basePlane for centered extrusion
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * dZ / 2));

            // base points for the tenons
            Point3d[] divPts = Joint.GetPoints(basePlane, minValue[0], tenonCount);

            // create the tenon joints
            for (int i = 0; i < divPts.Length; i++)
            {
                // set new base for origin
                basePlane.Origin = divPts[i];

                // create rectangles
                Rectangle3d firstRect = new Rectangle3d(basePlane, dY1, dX);
                Rectangle3d secondRect = new Rectangle3d(basePlane, dX, dY2);

                List<Curve> rectList = new List<Curve>
                {
                    firstRect.ToNurbsCurve(),
                    secondRect.ToNurbsCurve()
                };

                // join and fillet cross
                Curve cross = Curve.CreateBooleanUnion(rectList, 0.00001)[0];
                Curve baseCurve = Curve.CreateFilletCornersCurve(cross, toolRadius, 0.00001, 0.00001);

                // extrude first base to create first joint
                Brep joint = Extrusion.Create(baseCurve, dZ, true).ToBrep();

                // clean-up
                joint.Faces.SplitKinkyFaces(0.0001);
                if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                    joint.Flip();

                joints[i] = joint;
            }
        }


        //------------------------------------------------------------
        // CreateCustomTenon method
        //------------------------------------------------------------
        protected void CreateCustomTenon(Curve shape, Plane plane, double jointX, double jointY, double jointZ, double[] minValue, double toolRadius, int tenonCount, out Brep[] joints)
        {
            // initialise empty brep variable
            joints = new Brep[tenonCount];

            Curve jointShape = shape.DuplicateCurve();

            // get amp from curve
            var curveAMP = AreaMassProperties.Compute(jointShape);

            // new xy plane with curve centroid as origin
            Point3d curveCenter = curveAMP.Centroid;
            Plane curvePlane = new Plane(curveCenter, new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));

            // assign new plane
            Plane basePlane = new Plane(plane);

            // scale jointShape curve down
            Box box = new Box(jointShape.GetBoundingBox(true));
            jointShape.Transform(Transform.Scale(curvePlane, 1 / box.X.Length * jointX / 2, 1 / box.Y.Length * jointY / 2, 1));

            // z-value
            double dZ = jointZ;

            // move plane a bit for extrusion
            basePlane.Transform(Transform.Translation(-basePlane.ZAxis * dZ / 2));

            // base points for the dovetails
            Point3d[] divPts = Joint.GetPoints(basePlane, minValue[0], tenonCount);

            // create the tenon joints
            for (int i = 0; i < divPts.Length; i++)
            {
                // set new base for origin
                basePlane.Origin = divPts[i];

                // orient curvePlane to the base plane
                jointShape.DuplicateCurve().Transform(Transform.PlaneToPlane(curvePlane, basePlane));

                if (!jointShape.IsClosed)
                    jointShape.MakeClosed(0.0001);

                // fillet base curve
                Curve baseCurve = Curve.CreateFilletCornersCurve(jointShape, toolRadius, 0.0001, 0.0001);

                // extrude first base to create first joint
                Brep joint = Extrusion.Create(baseCurve, dZ, true).ToBrep();

                // clean-up
                joint.Faces.SplitKinkyFaces(0.0001);
                if (BrepSolidOrientation.Inward == joint.SolidOrientation)
                    joint.Flip();

                joints[i] = joint;
            }
        }


        //------------------------------------------------------------
        // Else
        //------------------------------------------------------------

        // exposure property
        public override GH_Exposure Exposure => GH_Exposure.secondary;

        // add icon
        protected override System.Drawing.Bitmap Icon => Properties.Resources._24x24_TenonJoints;

        // component giud
        public override Guid ComponentGuid => new Guid("0CAE8FA6-7CFE-4C2E-8E19-AE5F005EF8FB");
    }
}