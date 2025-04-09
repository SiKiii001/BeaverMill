using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using System.IO;



namespace BeaverMill
{
    public class BeaverMillComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public BeaverMillComponent()
          : base("DogBone", "DBone",
            "Creates Dogbone joints in the inner corners of a box joint or dovetail joint for CNC milling",
            "BeaverMill", "Joinery")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "Curve", "Input polyline", GH_ParamAccess.item);
            pManager.AddNumberParameter("Bit Diameter", "Dia", "Diameter of the CNC bit", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Dogbone Circles", "C", "Generated dogbone joint circles", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            double bitDiameter = 0.0;

            if (!DA.GetData(0, ref curve) || !DA.GetData(1, ref bitDiameter))
                return;

            if (!(curve is PolylineCurve polylineCurve))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input must be a polyline.");
                return;
            }

            if (!polylineCurve.TryGetPolyline(out Polyline polyline) || polyline.Count < 3)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid polyline.");
                return;
            }

            double radius = bitDiameter / 2.0;
            List<Curve> dogboneCircles = new List<Curve>();
            int pointCount = polyline.Count;

            for (int i = 0; i < pointCount; i++)
            {
                Point3d current = polyline[i];
                Point3d prev = polyline[(i - 1 + pointCount) % pointCount];
                Point3d next = polyline[(i + 1) % pointCount];

                Vector3d v1 = prev - current;
                Vector3d v2 = next - current;

                v1.Unitize();
                v2.Unitize();

                if (Vector3d.CrossProduct(v1, v2).Z > 0)
                {
                    Vector3d bisector = v1 + v2;
                    bisector.Unitize();

                    Point3d circleCenter = current + (bisector * radius);
                    Circle dogBone = new Circle(circleCenter, radius);

                    dogboneCircles.Add(dogBone.ToNurbsCurve());
                }
            }

            DA.SetDataList(0, dogboneCircles);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.Untitled1_01))
                {
                    Bitmap bmp = new Bitmap(ms);
                    return new Bitmap(bmp, new Size(24, 24));
                }
            }
        }
        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("37ab48b0-0ed3-4de1-ad96-77315b5b980e");
    }
}