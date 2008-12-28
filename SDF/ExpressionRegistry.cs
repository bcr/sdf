// ExpressionRegistry.cs created with MonoDevelop
// User: blake at 16:20 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Reflection;

namespace SDF
{
    public class ExpressionRegistry
    {
        private Hashtable expressions = new Hashtable();

        [SDFArgument(Name="filename")]
        private class LoadExpressions
        {
            public void PostCreateExpression(SDFState state, string name, Hashtable arguments, SDFParsedExpressionList children)
            {
                ((ExpressionRegistry) state[typeof(ExpressionRegistry)]).AddAssembly(arguments["filename"].ToString());
            }

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
            }
        }

        private class Expression
        {
            private SDFParsedExpressionList rootExpressionChildren = null;

            public object CreateExpression(SDFState state, string name, Hashtable arguments)
            {
                if (name == GetType().Name)
                {
                    object o = new Expression();
                    ((ExpressionRegistry) state[typeof(ExpressionRegistry)]).AddObject(arguments["name"].ToString(), o);
                    return o;
                }
                else
                {
                    return this;
                }
            }

            public void PostCreateExpression(SDFState state, string name, Hashtable arguments, SDFParsedExpressionList children)
            {
                if (name == GetType().Name)
                {
                    this.rootExpressionChildren = children;
                }
            }

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                if (name != GetType().Name)
                {
                    this.rootExpressionChildren.Evaluate(state);
                }
            }

            public static void Register(ExpressionRegistry registry)
            {
                registry.AddObject(typeof(Expression).Name, new Expression());
            }
        }

        private class TokenResult
        {
            private string result;

            public string Result
            {
                set { this.result = value; }
                get { return this.result; }
            }
        }

        [SDFArgument(Name="name")]
        [SDFStateProvided(typeof(TokenResult))]
        private class TokenExpression
        {
            private SDFParsedExpressionList rootExpressionChildren = null;
            TokenClass token;

            public TokenClass Token
            {
                get { return token; }
            }

            public TokenExpression()
            {
                this.token = new TokenClass(this);
            }

            [SDFArgument(Name="result")]
            private class SetTokenResult
            {
                [SDFStateRequired(typeof(TokenResult))]
                public void Evaluate(SDFState state, string name, Hashtable arguments)
                {
                    ((TokenResult) state[typeof(TokenResult)]).Result = arguments["result"].ToString();
                }
            }

            public class TokenClass
            {
                private TokenExpression parent;

                public TokenClass(TokenExpression parent)
                {
                    this.parent = parent;
                }

                public object CreateToken()
                {
                    return this;
                }

                public string Evaluate(SDFState state, ArrayList arguments)
                {
                    TokenResult result = new TokenResult();

                    state += result;
                    try
                    {
                        parent.EvaluateChildExpressions(state);
                    }
                    finally
                    {
                        state -= result;
                    }
                    return result.Result;
                }

            }

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
            }

            public void PostCreateExpression(SDFState state, string name, Hashtable arguments, SDFParsedExpressionList children)
            {
                this.rootExpressionChildren = children;
            }

            private void EvaluateChildExpressions(SDFState state)
            {
                this.rootExpressionChildren.Evaluate(state);
            }

            public static object CreateExpression(SDFState state, string name, Hashtable arguments)
            {
                TokenExpression o = new TokenExpression();
                ((TokenStringRegistry) state[typeof(TokenStringRegistry)]).AddObject(arguments["name"].ToString(), o.Token);
                return o;
            }

            public static void Register(ExpressionRegistry registry)
            {
                registry.AddType("Token", typeof(TokenExpression));
                registry.AddType("SetTokenResult", typeof(SetTokenResult));
            }
        }

        public void AddAssembly(string assemblyFilename)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyFilename);
            foreach (Type type in assembly.GetExportedTypes())
            {
                AddType(type);
            }
        }

        public void AddType(string name, Type type)
        {
            this[name] = type;
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
                return this.expressions[index];
            }

            set
            {
                this.expressions[index] = value;
            }
        }

        public ExpressionRegistry()
        {
            AddType(typeof(LoadExpressions));
            Expression.Register(this);
            TokenExpression.Register(this);
        }
    }
}
