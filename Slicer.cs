using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using System.Drawing;
using System.IO;

namespace BeaverMill
{
    public class Slicer : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Slicer class.
        /// </summary>
        public Slicer()
          : base("CNC Slicer", "Slicer",
              "slices 3D models into layers based on the available material thickness, considering CNC bed depth limitations. The sliced sections can be arranged on thinner sheets for cutting and later assembled to form the complete 3D model.",
              "BeaverMill", "Fabrication")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "Geo", "3D model to slice", GH_ParamAccess.item);
            pManager.AddNumberParameter("MaterialThickness", "T", "Slicing thickness", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Slices", "S", "Sliced Breps", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep brep = null;
            double thickness = 1.0;

            if (!DA.GetData(0, ref brep) || brep == null) return;
            if (!DA.GetData(1, ref thickness) || thickness <= 0) return;

            List<Brep> slices = SliceBrep(brep, thickness, RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? Rhino.RhinoMath.ZeroTolerance);
            DA.SetDataList(0, slices);
        }

        private List<Brep> SliceBrep(Brep brep, double thickness, double tol)
        {
            List<Brep> sliceBreps = new List<Brep>();
            BoundingBox bbox = brep.GetBoundingBox(true);
            double zMin = bbox.Min.Z;
            double zMax = bbox.Max.Z;
            int numSlices = (int)Math.Ceiling((zMax - zMin) / thickness);

            for (int i = 0; i < numSlices; i++)
            {
                double currentZMin = zMin + i * thickness;
                double currentZMax = Math.Min(currentZMin + thickness, zMax);
                BoundingBox sliceBbox = new BoundingBox(
                    new Point3d(bbox.Min.X, bbox.Min.Y, currentZMin),
                    new Point3d(bbox.Max.X, bbox.Max.Y, currentZMax));
                Box sliceBox = new Box(sliceBbox);
                Brep sliceBoxBrep = sliceBox.ToBrep();

                Brep[] intersected = Brep.CreateBooleanIntersection(
                    new List<Brep> { brep },
                    new List<Brep> { sliceBoxBrep },
                    tol);

                if (intersected != null && intersected.Length > 0)
                {
                    sliceBreps.AddRange(intersected);
                }
            }

            return sliceBreps;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                using (MemoryStream ms = new MemoryStream(Properties.Resources.Untitled1_02))
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
            get { return new Guid("54821FC4-5688-404E-872A-0BF3E298FB4C"); }
        }
    }
}