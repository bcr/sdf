// SDFStateRequired.cs created with MonoDevelop
// User: blake at 17:39 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace SDF
{
    public class SDFStateRequired : Attribute
    {
        private Type typeVar;

        public Type RequiredType
        {
            get
            {
                return this.typeVar;
            }

            set
            {
                this.typeVar = value;
            }
        }

        public SDFStateRequired(Type type)
        {
            RequiredType = type;
        }
    }
}
