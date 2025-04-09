using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace BeaverMill
{
    public class BeaverMillInfo : GH_AssemblyInfo
    {
        public override string Name => "BeaverMill";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("35828b83-729f-4d66-aa23-ac123d883847");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}