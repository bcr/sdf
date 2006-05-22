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
        private SDFState state;

        [SetUp]
        public void SetUp()
        {
            this.output = new StandardOutputRedirector();
            this.state = new SDFState();
            this.sdf = new SDF();
            this.sdf.Eval(this.state, "LoadExpressions filename='SDF.Print.dll'");
        }

        [TearDown]
        public void TearDown()
        {
            this.output.Unhook();
        }

        [Test]
        public void TestPrint()
        {
            this.sdf.Eval(this.state, "Print message='Hello, world'");

            Assert.AreEqual("Hello, world\n", this.output.ToString());
        }

        [Test]
        public void TestPrintUpper()
        {
            this.sdf.Eval(this.state, "PrintUpper message='Hello, world'");

            Assert.AreEqual("HELLO, WORLD\n", this.output.ToString());
        }

        [Test]
        public void TestTwoExpressions()
        {
            this.sdf.Eval(this.state,
                "Print message='Hello, world'\n" +
                "PrintUpper message='Hello, world'\n"
                );

            Assert.AreEqual("Hello, world\nHELLO, WORLD\n", this.output.ToString());
        }

        public class Foo
        {
            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                System.Console.WriteLine("I am Foo");
            }
        }

        [Test]
        public void TestTwoExpressionsNoParameters()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(Foo));

            this.sdf.Eval(
                state,
                "Foo\n" +
                "Foo\n"
                );

            Assert.AreEqual("I am Foo\nI am Foo\n", this.output.ToString());
        }

        public class FooAsFactory
        {
            private string name = null;

            public static object CreateExpression(SDFState state, string name, Hashtable arguments)
            {
                return new FooAsFactory(name);
            }

            private FooAsFactory(string name)
            {
                this.name = name;
            }

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                System.Console.WriteLine("I am {0}", this.name);
            }
        }

        [Test]
        public void TestFooAsFactory()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooAsFactory));

            this.sdf.Eval(state, "FooAsFactory");
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

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                System.Console.WriteLine("I am FooWithRequiredParam {0}", argumentVar);
            }
        }

        [Test]
        [ExpectedException(typeof(SDFException), @"Rquired argument 'argument' was not specified")]
        public void TestExpressionRequiredParamMissing()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredParam));

            this.sdf.Eval(this.state, "FooWithRequiredParam");
        }

        [Test]
        public void TestExpressionRequiredParamPresent()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredParam));

            this.sdf.Eval(this.state, "FooWithRequiredParam argument='hear me roar'");

            Assert.AreEqual("I am FooWithRequiredParam hear me roar\n", this.output.ToString());
        }

        public class FooWithRequiredState
        {
            [SDFStateRequired(typeof(string))]
            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                System.Console.WriteLine("I am FooWithRequiredState {0}", state[typeof(string)]);
            }
        }

        [Test]
        [ExpectedException(typeof(SDFException), "Required state 'String' was not found")]
        public void TestExpressionRequiredStateMissing()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredState));

            this.sdf.Eval(this.state, "FooWithRequiredState");
        }

        [Test]
        public void TestExpressionRequiredStatePresent()
        {
            this.state += "hear me roar";

            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredState));

            this.sdf.Eval(this.state, "FooWithRequiredState");

            Assert.AreEqual("I am FooWithRequiredState hear me roar\n", this.output.ToString());
        }

        [Test]
        [ExpectedException(typeof(SDFException), "Unknown expression 'Foo'")]
        public void TestExpressionNotFound()
        {
            this.sdf.Eval(this.state, "Foo");
        }

        private class FixedAnswer
        {
            private string isTrueVar = null;

            [SDFArgument(Required=false)]
            public string isTrue
            {
                set { this.isTrueVar = value; }
                get { return this.isTrueVar; }
            }

            public object Evaluate(SDFState state, string name, Hashtable arguments)
            {
                return isTrueVar;
            }
        }

        [Test]
        public void TestRunContainedExpressions()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FixedAnswer));

            this.sdf.Eval(
                this.state,
                "FixedAnswer\n" +
                "    Print message='Shouldnt get here. And fix the apostrophe.'\n" +
                "FixedAnswer isTrue='yep'\n" +
                "    Print message='isTrue is true'\n" +
                "Print message='Always output'\n"
                );

            Assert.AreEqual("isTrue is true\nAlways output\n", this.output.ToString());
        }

        private class ObjectBasedFactory
        {
            private class ObjectBasedFactoryInstance
            {
                public void Evaluate(SDFState state, string name, Hashtable arguments)
                {
                    Console.WriteLine("Dude this is {0}", arguments["message"]);
                }
            }

            public object CreateExpression(SDFState state, string name, Hashtable arguments)
            {
                return new ObjectBasedFactoryInstance();
            }
        }

        [Test]
        public void TestObjectBasedFactory()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddObject("ObjectBasedFactoryDude", new ObjectBasedFactory());

            this.sdf.Eval(this.state, "ObjectBasedFactoryDude message='yap'\n");

            Assert.AreEqual("Dude this is yap\n", this.output.ToString());
        }

        [Test]
        public void TestCustomExpressions()
        {
            this.sdf.Eval(
                this.state,
                "Expression name='Bar'\n" +
                "    Print message='Bar'\n" +
                "Expression name='Foo'\n" +
                "    Print message='Foo'\n" +
                "Foo\n" +
                "Bar\n" +
                "Foo\n"
                );

            Assert.AreEqual("Foo\nBar\nFoo\n", this.output.ToString());
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

        public SDFState()
        {
            // The default state includes an expression registry. This may need to be refactored.

            AddState(new SDFExpressionRegistry());
        }
    }

    public class SDFExpressionRegistry
    {
        private Hashtable expressions = new Hashtable();

        private class LoadExpressions
        {
            [SDFArgument(Required=true)]
            public string filename
            {
                set
                {
                }
            }

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                ((SDFExpressionRegistry) state[typeof(SDFExpressionRegistry)]).AddAssembly((string) arguments["filename"]);
            }
        }

        private class Expression
        {
            private SDF.SDFParsedExpressionList children = null;

            public object CreateExpression(SDFState state, string name, Hashtable arguments)
            {
                if (name == GetType().Name)
                {
                    object o = new Expression();
                    ((SDFExpressionRegistry) state[typeof(SDFExpressionRegistry)]).AddObject((string) arguments["name"], o);
                    return o;
                }
                else
                {
                    return this;
                }
            }

            public void PostCreateExpression(SDF.SDFParsedExpressionList children)
            {
                this.children = children;
            }

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                if (name != GetType().Name)
                {
                    this.children.Evaluate(state);
                }
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

        public SDFExpressionRegistry()
        {
            AddType(typeof(LoadExpressions));
            AddObject(typeof(Expression).Name, new Expression());
        }
    }

    public class SDF
    {
        public SDF()
        {
        }

        public class SDFParsedExpression
        {
            private string expressionNameVar = null;
            private Hashtable argumentsVar = null;
            private int indentLevelVar = 0;
            private Object expressionVar = null;

            public string ExpressionName
            {
                get
                {
                    return this.expressionNameVar;
                }
            }

            public Hashtable Arguments
            {
                get
                {
                    return this.argumentsVar;
                }
            }

            public int IndentLevel
            {
                get
                {
                    return this.indentLevelVar;
                }
            }

            public Object Expression
            {
                get
                {
                    return this.expressionVar;
                }

                set
                {
                    this.expressionVar = value;
                }
            }

            public SDFParsedExpression(string expressionName, Hashtable arguments, int indentLevel)
            {
                this.expressionNameVar = expressionName;
                this.argumentsVar = arguments;
                this.indentLevelVar = indentLevel;
            }
        }

        [TestFixture]
        public class TestSDFExpressionParser
        {
            [Test]
            public void TestIndentLevel()
            {
                SDFParsedExpressionList expressionList;
                SDFParsedExpression fooExpression;
                SDFParsedExpression barExpression;
                SDFParsedExpression bazExpression;
                IEnumerator enumerator = null;

                expressionList = SDFExpressionParser.Parse(
                    "Foo arg1='1'\n" +
                    " arg2='2'\n" +
                    "\n" +
                    "    \n" +
                    "    Bar arg1='1'\n" +
                    "Baz\n"
                    );

                enumerator = expressionList.GetEnumerator();
                Assert.IsTrue(enumerator.MoveNext());
                fooExpression = (SDFParsedExpression) enumerator.Current;
                Assert.IsTrue(enumerator.MoveNext());

                {
                    SDFParsedExpressionList fooChildren = (SDFParsedExpressionList) enumerator.Current;
                    Assert.IsTrue(enumerator.MoveNext());
                    IEnumerator fooEnumerator = fooChildren.GetEnumerator();
                    Assert.IsTrue(fooEnumerator.MoveNext());
                    barExpression = (SDFParsedExpression) fooEnumerator.Current;
                    Assert.IsFalse(fooEnumerator.MoveNext());
                }

                bazExpression = (SDFParsedExpression) enumerator.Current;
                Assert.IsFalse(enumerator.MoveNext());

                Assert.AreEqual("Foo", fooExpression.ExpressionName);
                Assert.AreEqual("Bar", barExpression.ExpressionName);
                Assert.AreEqual("Baz", bazExpression.ExpressionName);

                Assert.IsTrue(fooExpression.IndentLevel == bazExpression.IndentLevel);
                Assert.IsTrue(barExpression.IndentLevel > bazExpression.IndentLevel);
            }
        }

        private class SDFExpressionParser
        {
            public static SDFParsedExpressionList Parse(string expressions)
            {
                Regex regex = new Regex(@"\n?(?<indent>[ \t]*)(?<expression>\S+)(\s+(?<name>[^ \t=]+)\s*=\s*'(?<value>[^']+)')*");
                SDFParsedExpressionList currentList = new SDFParsedExpressionList();
                SDFParsedExpressionList rootList = currentList;
                int lastIndentLevel = -1;
                Stack expressionListStack = new Stack();

                foreach (Match match in regex.Matches(expressions))
                {
                    string expression = null;
                    Hashtable arguments = null;
                    int currentIndentLevel = match.Groups["indent"].Length;

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

                    if (lastIndentLevel == -1)
                    {
                        lastIndentLevel = currentIndentLevel;
                    }

                    if (currentIndentLevel > lastIndentLevel)
                    {
                        SDFParsedExpressionList lastList = currentList;

                        // Push the current guy on the stack, set up a new one

                        expressionListStack.Push(currentList);
                        currentList = new SDFParsedExpressionList();
                        lastList += currentList;
                    }
                    else if (currentIndentLevel < lastIndentLevel)
                    {
                        // Pop lists until we get to this level
                        while (currentIndentLevel < currentList.IndentLevel)
                        {
                            currentList = (SDFParsedExpressionList) expressionListStack.Pop();
                        }
                    }

                    currentList += new SDFParsedExpression(expression, arguments, currentIndentLevel);
                    lastIndentLevel = currentIndentLevel;
                }

                return rootList;
            }

            public IEnumerator GetEnumerator()
            {
                return null;
            }
        }

        public class SDFParsedExpressionList : IEnumerable
        {
            private ArrayList expressionList = new ArrayList();

            public int IndentLevel
            {
                get
                {
                    return ((SDFParsedExpression) expressionList[0]).IndentLevel;
                }
            }

            public static SDFParsedExpressionList operator+(SDFParsedExpressionList list, SDFParsedExpression parsedExpression)
            {
                list.AddExpression(parsedExpression);
                return list;
            }

            public static SDFParsedExpressionList operator+(SDFParsedExpressionList list, SDFParsedExpressionList parsedExpressionList)
            {
                list.AddExpressionList(parsedExpressionList);
                return list;
            }

            public void AddExpression(SDFParsedExpression parsedExpression)
            {
                this.expressionList.Add(parsedExpression);
            }

            public void AddExpressionList(SDFParsedExpressionList parsedExpressionList)
            {
                this.expressionList.Add(parsedExpressionList);
            }

            public IEnumerator GetEnumerator()
            {
                return this.expressionList.GetEnumerator();
            }

            public static void Evaluate(SDFParsedExpressionList expressionList, SDFState state)
            {
                Object lastExpressionObject = null;

                foreach (Object o in expressionList)
                {
                    if (o is SDFParsedExpressionList)
                    {
                        if (lastExpressionObject != null)
                        {
                            lastExpressionObject = null;
                            Evaluate((SDFParsedExpressionList) o, state);
                        }
                    }
                    else
                    {
                        SDFParsedExpression expression = (SDFParsedExpression) o;
                        MethodInfo method = expression.Expression.GetType().GetMethod("Evaluate");

                        lastExpressionObject = method.Invoke(expression.Expression, new Object[] { state, expression.ExpressionName, expression.Arguments });
                    }
                }
            }

            public void Evaluate(SDFState state)
            {
                Evaluate(this, state);
            }
        }

        public void Load(SDFParsedExpressionList expressionList, SDFState state)
        {
            SDFExpressionRegistry expressions = (SDFExpressionRegistry) state[typeof(SDFExpressionRegistry)];
            object newObject = null;

            foreach (Object o in expressionList)
            {
                if (o is SDFParsedExpressionList)
                {
                    MethodInfo postCreateMethod = newObject.GetType().GetMethod("PostCreateExpression");
                    if (postCreateMethod != null)
                    {
                        postCreateMethod.Invoke(newObject, new Object[] { o });
                    }
                    Load((SDFParsedExpressionList) o, state);
                }
                else
                {
                    SDFParsedExpression expression = (SDFParsedExpression) o;

                    {
                        object foundObject = expressions[expression.ExpressionName];
                        Type type = foundObject as Type;

                        if ((foundObject != null) && (type == null))
                        {
                            type = foundObject.GetType();
                        }

                        if (type == null)
                        {
                            throw new SDFException(String.Format("Unknown expression '{0}'", expression.ExpressionName));
                        }

                        {
                            MethodInfo method = type.GetMethod("CreateExpression");

                            if (method != null)
                            {
                                newObject = method.Invoke(foundObject, new Object[] { state, expression.ExpressionName, expression.Arguments });
                                type = newObject.GetType();
                            }
                            else
                            {
                                newObject = type.GetConstructor(new Type[0]).Invoke(null);
                            }
                        }

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

                                    property.GetSetMethod().Invoke(newObject, new Object[] { expression.Arguments[property.Name] });
                                }
                            }
                        }

                        {
                            MethodInfo method = type.GetMethod("Evaluate");

                            // Now check to see if there's any required state

                            foreach (SDFStateRequired stateRequired in method.GetCustomAttributes(typeof(SDFStateRequired), false))
                            {
                                if (state[stateRequired.RequiredType] == null)
                                {
                                    throw new SDFException(String.Format("Required state '{0}' was not found", stateRequired.RequiredType.Name));
                                }
                            }
                        }

                        expression.Expression = newObject;
                    }
                }
            }
        }

        public void Eval(SDFState state, string eval)
        {
            SDFParsedExpressionList expressionList = SDFExpressionParser.Parse(eval);
            Load(expressionList, state);
            expressionList.Evaluate(state);
        }
    }
}
