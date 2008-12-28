// TokenStringRegsistry.cs created with MonoDevelop
// User: blake at 16:27 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Reflection;

namespace SDF
{
    public class TokenStringRegistry
    {
        private Hashtable tokens = new Hashtable();

        public void AddAssembly(string assemblyFilename)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyFilename);
            foreach (Type type in assembly.GetExportedTypes())
            {
                AddType(type);
            }
        }

        public void AddType(Type type)
        {
            this[type.Name] = type;
        }

        public void AddObject(string name, object o)
        {
            this[name] = o;
        }

        public object this[string index]
        {
            get
            {
                return this.tokens[index];
            }

            set
            {
                this.tokens[index] = value;
            }
        }

        public object NewObject(string name)
        {
            object foundObject = this[name];
            Type type = foundObject as Type;

            if (foundObject == null)
            {
                throw new SDFException(String.Format("Unknown token '{0}'", name));
            }

            if (type != null)
            {
                return type.GetConstructor(new Type[0]).Invoke(null);
            }
            else
            {
                return foundObject.GetType().GetMethod("CreateToken").Invoke(foundObject, null);
            }
        }

        public TokenStringRegistry()
        {
        }
    }
}
