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
        private bool requiredVar = true;
        private string nameVar = null;

        public bool Required
        {
            get
            {
                return requiredVar;
            }

            set
            {
                requiredVar = value;
            }
        }

        public string Name
        {
            get
            {
                return nameVar;
            }

            set
            {
                nameVar = value;
            }
        }
    }
}
