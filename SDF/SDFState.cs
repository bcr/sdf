// SDFState.cs created with MonoDevelop
// User: blake at 17:41 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;

namespace SDF
{
    public class SDFState
    {
        private ArrayList state = new ArrayList();

        public object this[Type type]
        {
            get
            {
                Object found = null;

                foreach (Object o in state)
                {
                    if (o.GetType() == type)
                    {
                        found = o;
                    }
                }

                return found;
            }
        }

        public void AddState(object o)
        {
            this.state.Add(o);
        }

        public static SDFState operator+(SDFState state, Object o)
        {
            state.AddState(o);
            return state;
        }

        public void RemoveState(object o)
        {
            this.state.Remove(o);
        }

        public static SDFState operator-(SDFState state, Object o)
        {
            state.RemoveState(o);
            return state;
        }

        public SDFState()
        {
            // The default state includes an expression registry and a token registry.
            // This may need to be refactored.

            AddState(new ExpressionRegistry());
            AddState(new TokenStringRegistry());
        }
    }
}
