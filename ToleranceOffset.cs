using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;
using System.IO;

namespace BeaverMill
{
    public class ToleranceOffset : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ToleranceOffset class.
        /// </summary>
        public ToleranceOffset()
          : base("Tolerance", "Offset",
              "Adds tolerance for lasercutting or cnc",
              "BeaverMill", "Fabrication")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Closed curve to offset", GH_ParamAccess.item);
            pManager.AddNumberParameter("Thickness", "T", "Material thickness", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Inside Cut", "IC", "Offset inside (true) or outside (false)", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Offset Curve", "OC", "Offset curve with kerf adjustment", GH_ParamAccess.item);
            pManager.AddNumberParameter("Kerf", "K", "Calculated kerf value", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            double thickness = 0;
            bool insideCut = false;

            if (!DA.GetData(0, ref crv)) return;
            if (!DA.GetData(1, ref thickness)) return;
            if (!DA.GetData(2, ref insideCut)) return;

            if (crv == null || !crv.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid or open curve!");
                return;
            }

            double kerf = CalculateKerf(thickness);
            double offsetDistance = insideCut ? -kerf / 2.0 : kerf / 2.0;

            Plane plane;
            if (!crv.TryGetPlane(out plane))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not determine curve plane!");
                return;
            }

            Curve[] offsets = crv.Offset(plane, offsetDistance, 0.001, CurveOffsetCornerStyle.Sharp);

            if (offsets == null || offsets.Length == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Offset operation failed!");
                return;
            }

            DA.SetData(0, offsets[0]);
            DA.SetData(1, kerf);
        }

        private double CalculateKerf(double thickness)
        {
            if (thickness <= 1.5) return 0.12;
            else if (thickness <= 2.0) return 0.15;
            else if (thickness <= 2.5) return 0.18;
            else return 0.2;

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.images_01))
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
            get { return new Guid("F351E538-C978-4405-B9E7-56E650E01AAE"); }
        }
    }
}