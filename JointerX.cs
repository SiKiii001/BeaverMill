using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.IO;


namespace BeaverMill
{
    public class MyComponent1 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MyComponent1()
          : base("JointerX", "JX",
              "Creates Box & Finger Joint",
              "BeaverMill", "Joinery")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Surface A", "A", "First Brep surface", GH_ParamAccess.item);
            pManager.AddBrepParameter("Surface B", "B", "Second Brep surface", GH_ParamAccess.item);
            pManager.AddNumberParameter("Finger Gap", "FG", "Gap between fingers", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Finger Depth", "FD", "Depth of the fingers", GH_ParamAccess.item, 5.0);
            pManager.AddIntegerParameter("Number of Fingers", "N", "Number of fingers", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Flip Direction", "Flip", "Flip the direction of fingers", GH_ParamAccess.item, false);
            pManager.AddCurveParameter("Intersection Curve", "C", "Curve defining the intersection of surfaces", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rotation Angle A", "RA", "Rotation angle for surface A", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Rotation Angle B", "RB", "Rotation angle for surface B", GH_ParamAccess.item, 0.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Surface A Cutouts", "CA", "Cutout curves for surface A", GH_ParamAccess.list);
            pManager.AddCurveParameter("Surface B Cutouts", "CB", "Cutout curves for surface B", GH_ParamAccess.list);
            pManager.AddBrepParameter("Rotated Surface A", "RA", "Rotated version of Surface A", GH_ParamAccess.item);
            pManager.AddBrepParameter("Rotated Surface B", "RB", "Rotated version of Surface B", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep surfaceA = null, surfaceB = null;
            double fingerGap = 1.0, fingerDepth = 5.0, rotationAngleA = 0.0, rotationAngleB = 0.0;
            int numFingers = 5;
            bool flipDirection = false;
            Curve intersectionCurve = null;

            if (!DA.GetData(0, ref surfaceA) || !DA.GetData(1, ref surfaceB) ||
                !DA.GetData(2, ref fingerGap) || !DA.GetData(3, ref fingerDepth) ||
                !DA.GetData(4, ref numFingers) || !DA.GetData(5, ref flipDirection) ||
                !DA.GetData(6, ref intersectionCurve) ||
                !DA.GetData(7, ref rotationAngleA) || !DA.GetData(8, ref rotationAngleB))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid inputs!");
                return;
            }

            List<Curve> cutoutsA = new List<Curve>();
            List<Curve> cutoutsB = new List<Curve>();

            Point3d rotationCenter = intersectionCurve.PointAtNormalizedLength(0.5);
            Vector3d axis = intersectionCurve.TangentAt(intersectionCurve.Domain.Mid);
            axis.Unitize();

            double angleA_rad = RhinoMath.ToRadians(rotationAngleA);
            double angleB_rad = RhinoMath.ToRadians(rotationAngleB);

            Transform rotationA = Transform.Rotation(angleA_rad, axis, rotationCenter);
            Transform rotationB = Transform.Rotation(angleB_rad, axis, rotationCenter);

            Brep rotatedA = surfaceA.DuplicateBrep();
            Brep rotatedB = surfaceB.DuplicateBrep();
            rotatedA.Transform(rotationA);
            rotatedB.Transform(rotationB);

            double totalLength = intersectionCurve.GetLength();
            double baseWidth = totalLength / numFingers;
            double expandFactor = fingerGap / 2.0;
            double currentLength = 0.0;

            for (int i = 0; i < numFingers; i++)
            {
                double widthOffset = (i % 2 == 0) ? expandFactor : -expandFactor;
                double thisWidth = baseWidth + widthOffset;

                if (currentLength + thisWidth > totalLength)
                {
                    thisWidth = totalLength - currentLength;
                    if (thisWidth <= 0) break;
                }

                intersectionCurve.LengthParameter(currentLength, out double tStart);
                intersectionCurve.LengthParameter(currentLength + thisWidth, out double tEnd);

                double tMid = (tStart + tEnd) / 2.0;
                Point3d midPt = intersectionCurve.PointAt(tMid);
                Vector3d tan = intersectionCurve.TangentAt(tMid);
                tan.Unitize();

                Vector3d normal = new Vector3d(-tan.Y, tan.X, 0);
                normal.Unitize();
                normal *= fingerDepth;
                if (flipDirection) normal *= -1;

                Point3d ptStart = intersectionCurve.PointAt(tStart);
                Point3d ptEnd = intersectionCurve.PointAt(tEnd);
                Vector3d half = (ptEnd - ptStart) / 2.0;

                Point3d pt1 = midPt - half;
                Point3d pt2 = pt1 + normal;
                Point3d pt3 = midPt + half + normal;
                Point3d pt4 = midPt + half;

                Polyline fingerProfile = new Polyline(new List<Point3d> { pt1, pt2, pt3, pt4, pt1 });

                if (i % 2 == 0)
                {
                    fingerProfile.Transform(rotationA);
                    cutoutsA.Add(fingerProfile.ToNurbsCurve());
                }
                else
                {
                    Transform rotate90 = Transform.Rotation(Math.PI / 2, tan, midPt);
                    fingerProfile.Transform(rotate90);
                    fingerProfile.Transform(rotationB);
                    cutoutsB.Add(fingerProfile.ToNurbsCurve());
                }

                currentLength += thisWidth;
            }

            DA.SetDataList(0, cutoutsA);
            DA.SetDataList(1, cutoutsB);
            DA.SetData(2, rotatedA);
            DA.SetData(3, rotatedB);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.images_02))
                {
                    Bitmap bmp = new Bitmap(ms);
                    return new Bitmap(bmp, new Size(24, 24));
                }
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4E699814-09F4-4513-98F5-AEB9020E542B"); }
        }
    }
}