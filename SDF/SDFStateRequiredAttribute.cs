// SDFStateRequired.cs created with MonoDevelop
// User: blake at 17:39 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace SDF
{
    public class SDFStateRequiredAttribute : Attribute
    {
        public Type RequiredType
        {
            get; set;
        }

        public SDFStateRequiredAttribute(Type type)
        {
            RequiredType = type;
        }
    }
}
