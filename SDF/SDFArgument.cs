// SDFArgument.cs created with MonoDevelop
// User: blake at 17:36 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace SDF
{
    public class SDFArgument : Attribute
    {
        public bool Required
        {
            get; set;
        }

        public string Name
        {
            get; set;
        }

        public SDFArgument()
        {
            Required = true;
        }
    }
}
