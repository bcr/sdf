// SDFStateProvided.cs created with MonoDevelop
// User: blake at 17:40 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace SDF
{
    public class SDFStateProvidedAttribute : Attribute
    {
        public Type ProvidedType
        {
            get; set;
        }

        public SDFStateProvidedAttribute(Type type)
        {
            ProvidedType = type;
        }
    }
}
