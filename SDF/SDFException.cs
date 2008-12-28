// SDFException.cs created with MonoDevelop
// User: blake at 17:37 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;

namespace SDF
{
    public class SDFException : ApplicationException
    {
        public SDFException(string reason) : base(reason)
        {
        }
    }
}
