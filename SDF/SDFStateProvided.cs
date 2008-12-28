// SDFStateProvided.cs created with MonoDevelop
// User: blake at 17:40 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace SDF
{
    public class SDFStateProvided : Attribute
    {
        private Type typeVar;

        public Type ProvidedType
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

        public SDFStateProvided(Type type)
        {
            ProvidedType = type;
        }
    }
}
