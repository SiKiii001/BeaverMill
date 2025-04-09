using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System.Drawing;
using System.IO;


namespace BeaverMill
{
    public class Edge : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Edge class.
        /// </summary>
        public Edge()
          : base("MutalEdge", "MEdge",
              "Detects the common edge",
              "BeaverMill", "Elements")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Surface A", "A", "First surface", GH_ParamAccess.item);
            pManager.AddBrepParameter("Surface B", "B", "Second surface", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Intersection Curves", "C", "Intersection curves", GH_ParamAccess.list);
        }
        

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep surfaceA = null;
            Brep surfaceB = null;

            if (!DA.GetData(0, ref surfaceA) || !DA.GetData(1, ref surfaceB))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid input surfaces");
                return;
            }

            Surface srfA = surfaceA.Faces[0];
            Surface srfB = surfaceB.Faces[0];

            Curve[] intersectionCurves;
            Point3d[] intersectionPoints;

            bool success = Intersection.SurfaceSurface(srfA, srfB, 0.01, out intersectionCurves, out intersectionPoints);

            if (success && intersectionCurves != null && intersectionCurves.Length > 0)
            {
                DA.SetDataList(0, intersectionCurves);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No intersection found");
            }
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.elemts_01))
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
            get { return new Guid("E9201CBC-663A-47D0-9288-975CD24EC6D2"); }
        }
    }
}