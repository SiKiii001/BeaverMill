using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using System.IO;    

namespace BeaverMill
{
    public class Trim : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Trim class.
        /// </summary>
        public Trim()
          : base("SnipSurf", "SS",
              "Curves Trims the surface to get finger joints ",
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
            pManager.AddCurveParameter("Surface A Cutouts", "CA", "Cutout curves for Surface A", GH_ParamAccess.list);
            pManager.AddCurveParameter("Surface B Cutouts", "CB", "Cutout curves for Surface B", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Trimmed Surface A", "TA", "Trimmed Surface A", GH_ParamAccess.item);
            pManager.AddBrepParameter("Trimmed Surface B", "TB", "Trimmed Surface B", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep surfaceA = null;
            Brep surfaceB = null;
            List<Curve> surfaceA_Cutouts = new List<Curve>();
            List<Curve> surfaceB_Cutouts = new List<Curve>();

            if (!DA.GetData(0, ref surfaceA) || !DA.GetData(1, ref surfaceB))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid input surfaces!");
                return;
            }

            DA.GetDataList(2, surfaceA_Cutouts);
            DA.GetDataList(3, surfaceB_Cutouts);

            if ((surfaceA_Cutouts == null || surfaceA_Cutouts.Count == 0) && (surfaceB_Cutouts == null || surfaceB_Cutouts.Count == 0))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No cutouts provided!");
                return;
            }

            Brep trimmedA = TrimSurface(surfaceA, surfaceA_Cutouts);
            Brep trimmedB = TrimSurface(surfaceB, surfaceB_Cutouts);

            DA.SetData(0, trimmedA);
            DA.SetData(1, trimmedB);
        }

        private Brep TrimSurface(Brep surface, List<Curve> cutouts)
        {
            Brep[] split = surface.Split(cutouts, 0.01);
            if (split != null && split.Length > 0)
            {
                return GetLargestBrep(split);
            }
            return null;
        }

        private Brep GetLargestBrep(Brep[] breps)
        {
            Brep largest = null;
            double maxArea = 0;

            foreach (Brep b in breps)
            {
                AreaMassProperties areaProps = AreaMassProperties.Compute(b);
                if (areaProps != null)
                {
                    double area = areaProps.Area;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        largest = b;
                    }
                }
            }
            return largest;
        
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.elemts_02))
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
            get { return new Guid("2FE1C814-48A9-49EE-9ED9-509278B618AB"); }
        }
    }
}