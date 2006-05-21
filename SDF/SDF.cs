namespace SDF
{
    using NUnit.Framework;

    using System;
    using System.Collections;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Reflection;

    [TestFixture]
    public class TestEval
    {
        private class StandardOutputRedirector
        {
            private StringWriter outputWriter;
            private TextWriter oldWriter;

            public StandardOutputRedirector()
            {
                this.oldWriter = Console.Out;
                this.outputWriter = new StringWriter();
                Console.SetOut(this.outputWriter);
            }

            public void Unhook()
            {
                Console.SetOut(this.oldWriter);
            }

            public override string ToString()
            {
                return this.outputWriter.ToString();
            }
        }

        private StandardOutputRedirector output;
        private SDF sdf;

        [SetUp]
        public void SetUp()
        {
            this.output = new StandardOutputRedirector();
            this.sdf = new SDF();
            this.sdf.Eval(null, "LoadExpressions filename='SDF.Print.dll'");
        }

        [TearDown]
        public void TearDown()
        {
            this.output.Unhook();
        }

        [Test]
        public void TestPrint()
        {
            this.sdf.Eval(null, "Print message='Hello, world'");

            Assert.AreEqual("Hello, world\n", this.output.ToString());
        }

        [Test]
        public void TestPrintUpper()
        {
            this.sdf.Eval(null, "PrintUpper message='Hello, world'");

            Assert.AreEqual("HELLO, WORLD\n", this.output.ToString());
        }

        [Test]
        public void TestTwoExpressions()
        {
            this.sdf.Eval(null,
                "Print message='Hello, world'\n" +
                "PrintUpper message='Hello, world'\n"
                );

            Assert.AreEqual("Hello, world\nHELLO, WORLD\n", this.output.ToString());
        }

        public class Foo
        {
            public void Evaluate(SDF sdf, SDFState state, Hashtable arguments)
            {
                System.Console.WriteLine("I am Foo");
            }
        }

        [Test]
        public void TestTwoExpressionsNoParameters()
        {
            this.sdf.AddType(typeof(Foo));

            this.sdf.Eval(null,
                "Foo\n" +
                "Foo\n"
                );

            Assert.AreEqual("I am Foo\nI am Foo\n", this.output.ToString());
        }

        public class FooWithRequiredParam
        {
            string argumentVar;

            [SDFArgument(Required=true)]
            public string argument
            {
                set
                {
                    argumentVar = value;
                }
            }

            public void Evaluate(SDF sdf, SDFState state, Hashtable arguments)
            {
                System.Console.WriteLine("I am FooWithRequiredParam {0}", argumentVar);
            }
        }

        [Test]
        [ExpectedException(typeof(SDFException))]
        public void TestExpressionRequiredParamMissing()
        {
            this.sdf.AddType(typeof(FooWithRequiredParam));

            this.sdf.Eval(null, "FooWithRequiredParam");
        }

        [Test]
        public void TestExpressionRequiredParamPresent()
        {
            this.sdf.AddType(typeof(FooWithRequiredParam));

            this.sdf.Eval(null, "FooWithRequiredParam argument='hear me roar'");

            Assert.AreEqual("I am FooWithRequiredParam hear me roar\n", this.output.ToString());
        }

        public class FooWithRequiredState
        {
            [SDFStateRequired(typeof(string))]
            public void Evaluate(SDF sdf, SDFState state, Hashtable arguments)
            {
                System.Console.WriteLine("I am FooWithRequiredState {0}", state[typeof(string)]);
            }
        }

        [Test]
        [ExpectedException(typeof(SDFException))]
        public void TestExpressionRequiredStateMissing()
        {
            SDFState state = new SDFState();

            this.sdf.AddType(typeof(FooWithRequiredState));

            this.sdf.Eval(state, "FooWithRequiredState");
        }

        [Test]
        public void TestExpressionRequiredStatePresent()
        {
            SDFState state = new SDFState();

            state += "hear me roar";

            this.sdf.AddType(typeof(FooWithRequiredState));

            this.sdf.Eval(state, "FooWithRequiredState");

            Assert.AreEqual("I am FooWithRequiredState hear me roar\n", this.output.ToString());
        }

        [Test]
        [ExpectedException(typeof(SDFException))]
        public void TestExpressionNotFound()
        {
            this.sdf.Eval(null, "Foo");
        }

/*
        [Test]
        public void TestCustomExpressions()
        {
            SDFState state = new SDFState();

            this.sdf.Eval(
                state,
                "Expression name='Foo'\n" +
                "    Print message='Foo'\n" +
                "Expression name='Bar'\n" +
                "    Print message='Bar'\n" +
                "Foo\n" +
                "Bar\n"
                );

            Assert.AreEqual("Foo\nBar\n", this.output.ToString());
        }
*/
    }

    public class Expression
    {
        [SDFArgument(Required=true)]
        public string name
        {
            set
            {
            }
        }

        public void Evaluate(SDF sdf, SDFState state, Hashtable arguments)
        {
        }
    }

    public class SDFArgument : Attribute
    {
        private bool requiredVar;

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
    }

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

    public class SDFException : ApplicationException
    {
        public SDFException(string reason) : base(reason)
        {
        }
    }

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
    }

    public class SDF
    {
        private Hashtable expressions = new Hashtable();

        private class LoadExpressions
        {
            public void Evaluate(SDF sdf, SDFState state, Hashtable arguments)
            {
                sdf.AddAssembly((string) arguments["filename"]);
            }
        }

        private class Expression
        {
            [SDFArgument(Required=true)]
            public string name
            {
                set
                {
                }
            }

            public void Evaluate(SDF sdf, SDFState state, Hashtable arguments)
            {
            }
        }

        public SDF()
        {
            AddType(typeof(LoadExpressions));
            AddType(typeof(Expression));
        }

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
            expressions[type.Name] = type;
        }

        private class SDFParsedExpression
        {
            private string expressionVar = null;
            private Hashtable argumentsVar = null;
            private IComparable indentLevelVar = null;

            public string Expression
            {
                get
                {
                    return this.expressionVar;
                }
            }

            public Hashtable Arguments
            {
                get
                {
                    return this.argumentsVar;
                }
            }

            public IComparable IndentLevel
            {
                get
                {
                    return this.indentLevelVar;
                }
            }

            public SDFParsedExpression(string expression, Hashtable arguments, IComparable indentLevel)
            {
                this.expressionVar = expression;
                this.argumentsVar = arguments;
                this.indentLevelVar = indentLevel;
            }
        }

        private class SDFParsedExpressionGenerator : IEnumerable
        {
            private ArrayList expressions = null;

            public SDFParsedExpressionGenerator(string expressions)
            {
                Regex regex = new Regex(@"\s*(?<expression>\S+)(\s+(?<name>[^ \t=]+)\s*=\s*'(?<value>[^']+)')*");

                this.expressions = new ArrayList();
                foreach (Match match in regex.Matches(expressions))
                {
                    string expression = null;
                    Hashtable arguments = null;

                    {
                        Group names = match.Groups["name"];
                        Group values = match.Groups["value"];

                        expression = match.Groups["expression"].ToString();
                        arguments = new Hashtable(names.Captures.Count);

                        for (int counter = 0;counter < names.Captures.Count;++counter)
                        {
                            arguments[names.Captures[counter].ToString()] = values.Captures[counter].ToString();
                        }
                    }

                    this.expressions.Add(new SDFParsedExpression(expression, arguments, 0));
                }
            }

            public IEnumerator GetEnumerator()
            {
                return this.expressions.GetEnumerator();
            }
        }

        public void Eval(SDFState state, string eval)
        {
            foreach (SDFParsedExpression expression in new SDFParsedExpressionGenerator(eval))
            {
                {
                    Type type = (Type) expressions[expression.Expression];

                    if (type == null)
                    {
                        throw new SDFException(String.Format("Unknown expression '{0}'", expression.Expression));
                    }

                    object o = type.GetConstructor(new Type[0]).Invoke(null);

                    // Set arguments to properties if required

                    {
                        // For each property, check to see if it has an SDFArgument attribute

                        foreach (PropertyInfo property in type.GetProperties())
                        {
                            foreach (SDFArgument argument in property.GetCustomAttributes(typeof(SDFArgument), false))
                            {
                                // If the argument is required, then whine if it wasn't specified

                                if ((argument.Required) && (!expression.Arguments.Contains(property.Name)))
                                {
                                    throw new SDFException(String.Format("Rquired argument '{0}' was not specified", property.Name));
                                }

                                // Set the property

                                property.GetSetMethod().Invoke(o, new Object[] { expression.Arguments[property.Name] });
                            }
                        }
                    }

                    MethodInfo method = type.GetMethod("Evaluate");

                    {
                        // Now check to see if there's any required state

                        foreach (SDFStateRequired stateRequired in method.GetCustomAttributes(typeof(SDFStateRequired), false))
                        {
                            if (state[stateRequired.RequiredType] == null)
                            {
                                throw new SDFException(String.Format("Required state '{0}' was not found", stateRequired.RequiredType.Name));
                            }
                        }
                    }

                    // Finally call the method

                    method.Invoke(o, new Object[] { this, state, expression.Arguments });
                }
            }
        }
    }
}
