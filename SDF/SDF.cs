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
        private SDFState state;

        [SetUp]
        public void SetUp()
        {
            this.output = new StandardOutputRedirector();
            this.state = new SDFState();
            SDF.Eval(this.state, "LoadExpressions filename='SDF.Print.dll'");
        }

        [TearDown]
        public void TearDown()
        {
            this.output.Unhook();
        }

        [Test]
        public void TestPrint()
        {
            SDF.Eval(this.state, "Print message='Hello, world'");

            Assert.AreEqual("Hello, world\n", this.output.ToString());
        }

        [Test]
        public void TestPrintUpper()
        {
            SDF.Eval(this.state, "PrintUpper message='Hello, world'");

            Assert.AreEqual("HELLO, WORLD\n", this.output.ToString());
        }

        [Test]
        public void TestTwoExpressions()
        {
            SDF.Eval(this.state,
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

            SDF.Eval(
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

            SDF.Eval(state, "FooAsFactory");
        }

        public class FooWithRequiredParam
        {
            SDFTokenString argumentVar;

            [SDFArgument]
            public SDFTokenString argument
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

            SDF.Eval(this.state, "FooWithRequiredParam");
        }

        [Test]
        public void TestExpressionRequiredParamPresent()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredParam));

            SDF.Eval(this.state, "FooWithRequiredParam argument='hear me roar'");

            Assert.AreEqual("I am FooWithRequiredParam hear me roar\n", this.output.ToString());
        }

        [SDFArgument(Name="argument")]
        public class FooWithRequiredParamClassLevel
        {
            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
                System.Console.WriteLine("I am FooWithRequiredParamClassLevel {0}", arguments["argument"]);
            }
        }

        [Test]
        [ExpectedException(typeof(SDFException), @"Rquired argument 'argument' was not specified")]
        public void TestExpressionRequiredParamClassLevelMissing()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredParamClassLevel));

            SDF.Eval(this.state, "FooWithRequiredParamClassLevel");
        }

        [Test]
        public void TestExpressionRequiredParamClassLevelPresent()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredParamClassLevel));

            SDF.Eval(this.state, "FooWithRequiredParamClassLevel argument='hear me roar'");

            Assert.AreEqual("I am FooWithRequiredParamClassLevel hear me roar\n", this.output.ToString());
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

            SDF.Eval(this.state, "FooWithRequiredState");
        }

        [Test]
        public void TestExpressionRequiredStatePresent()
        {
            this.state += "hear me roar";

            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredState));

            SDF.Eval(this.state, "FooWithRequiredState");

            Assert.AreEqual("I am FooWithRequiredState hear me roar\n", this.output.ToString());
        }

        [SDFArgument(Name="string")]
        [SDFStateProvided(typeof(string))]
        private class StringState
        {
            public object Evaluate(SDFState state, string name, Hashtable arguments)
            {
                return arguments["string"].ToString();
            }
        }

        [Test]
        public void TestExpressionRequiredStateProvidedByParent()
        {
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(StringState));
            ((SDFExpressionRegistry) this.state[typeof(SDFExpressionRegistry)]).AddType(typeof(FooWithRequiredState));

            SDF.Eval(
                this.state,
                "StringState string='hear me roar'\n" +
                "    FooWithRequiredState\n"
                );

            Assert.AreEqual("I am FooWithRequiredState hear me roar\n", this.output.ToString());
        }

        [Test]
        [ExpectedException(typeof(SDFException), "Unknown expression 'Foo'")]
        public void TestExpressionNotFound()
        {
            SDF.Eval(this.state, "Foo");
        }

        public class upper
        {
            public string Evaluate(SDFState state, ArrayList arguments)
            {
                return arguments[1].ToString().ToUpper();
            }
        }

        [Test]
        public void TestExpressionWithStringToken()
        {
            ((SDFTokenStringRegistry) this.state[typeof(SDFTokenStringRegistry)]).AddType(typeof(upper));

            SDF.Eval(this.state, "Print message='$[upper,foo]'");

            Assert.AreEqual("FOO\n", this.output.ToString());
        }

        private class FixedAnswer
        {
            private SDFTokenString isTrueVar = null;

            [SDFArgument(Required=false)]
            public SDFTokenString isTrue
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

            SDF.Eval(
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

            SDF.Eval(this.state, "ObjectBasedFactoryDude message='yap'\n");

            Assert.AreEqual("Dude this is yap\n", this.output.ToString());
        }

        [Test]
        public void TestCustomExpressions()
        {
            SDF.Eval(
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

        [Test]
        public void TestLoadAndPrint()
        {
            // The point here is that LoadExpressions needs to update the state
            // with the loaded expressions at load time, not at evaluate time

            SDF.Eval(
                new SDFState(),
                "LoadExpressions filename='SDF.Print.dll'\n" +
                "Print message='Foo'\n"
                );

            Assert.AreEqual("Foo\n", this.output.ToString());
        }

        private class ReturnValuesClass
        {
            public void Evaluate()
            {
            }

            public string ReturnEvaluate()
            {
                return "foo";
            }
        }

        [Test]
        public void TestReturnValues()
        {
            Assert.AreEqual(typeof(void), typeof(ReturnValuesClass).GetMethod("Evaluate").ReturnType);
            Assert.AreEqual(typeof(string), typeof(ReturnValuesClass).GetMethod("ReturnEvaluate").ReturnType);
        }

        [Test]
        public void TestCustomToken()
        {
            SDF.Eval(
                this.state,
                "Token name='test'\n" +
                "    SetTokenResult result='Hello from test'\n" +
                "Print message='I do say $[test]'\n"
                );

            Assert.AreEqual("I do say Hello from test\n", this.output.ToString());
        }
    }

    public class SDFArgument : Attribute
    {
        private bool requiredVar = true;
        private string nameVar = null;

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

        public string Name
        {
            get
            {
                return nameVar;
            }

            set
            {
                nameVar = value;
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

            AddState(new SDFExpressionRegistry());
            AddState(new SDFTokenStringRegistry());
        }
    }

    public class SDFExpressionRegistry
    {
        private Hashtable expressions = new Hashtable();

        [SDFArgument(Name="filename")]
        private class LoadExpressions
        {
            public void PostCreateExpression(SDFState state, string name, Hashtable arguments, SDF.SDFParsedExpressionList children)
            {
                ((SDFExpressionRegistry) state[typeof(SDFExpressionRegistry)]).AddAssembly(arguments["filename"].ToString());
            }

            public void Evaluate(SDFState state, string name, Hashtable arguments)
            {
            }
        }

        private class Expression
        {
            private SDF.SDFParsedExpressionList rootExpressionChildren = null;

            public object CreateExpression(SDFState state, string name, Hashtable arguments)
            {
                if (name == GetType().Name)
                {
                    object o = new Expression();
                    ((SDFExpressionRegistry) state[typeof(SDFExpressionRegistry)]).AddObject(arguments["name"].ToString(), o);
                    return o;
                }
                else
                {
                    return this;
                }
            }

            public void PostCreateExpression(SDFState state, string name, Hashtable arguments, SDF.SDFParsedExpressionList children)
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

            public static void Register(SDFExpressionRegistry registry)
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
            private SDF.SDFParsedExpressionList rootExpressionChildren = null;
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

            public void PostCreateExpression(SDFState state, string name, Hashtable arguments, SDF.SDFParsedExpressionList children)
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
                ((SDFTokenStringRegistry) state[typeof(SDFTokenStringRegistry)]).AddObject(arguments["name"].ToString(), o.Token);
                return o;
            }

            public static void Register(SDFExpressionRegistry registry)
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

        public SDFExpressionRegistry()
        {
            AddType(typeof(LoadExpressions));
            Expression.Register(this);
            TokenExpression.Register(this);
        }
    }

    public class SDF
    {
        private SDF()
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
                            state += lastExpressionObject;
                            try
                            {
                                Evaluate((SDFParsedExpressionList) o, state);
                            }
                            finally
                            {
                                state -= lastExpressionObject;
                            }
                            lastExpressionObject = null;
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

        private static void MaybeCallPostCreateExpression(SDFState state, SDFParsedExpression expression, SDFParsedExpressionList children)
        {
            MethodInfo postCreateMethod = expression.Expression.GetType().GetMethod("PostCreateExpression");
            if (postCreateMethod != null)
            {
                postCreateMethod.Invoke(expression.Expression, new Object[] { state, expression.ExpressionName, expression.Arguments, children });
            }
        }

        private static void BindArguments(Hashtable arguments, SDFTokenStringRegistry tokenStringRegistry)
        {
            Hashtable newArguments = new Hashtable();

            // Not sure if this is stupid or not. I can't modify the collection in place,
            // so I use a copy and then copy it back.

            foreach (string key in arguments.Keys)
            {
                newArguments[key] = SDFTokenString.Parse(tokenStringRegistry, arguments[key].ToString());
            }

            foreach (string key in newArguments.Keys)
            {
                arguments[key] = newArguments[key];
            }
        }

        public class ProvidedStatePile
        {
            private ArrayList providedState = new ArrayList();
            private Stack indexStack = new Stack();

            public void AddProvidedStateFromObject(Object o)
            {
                this.indexStack.Push(providedState.Count);
                foreach (SDFStateProvided stateProvided in o.GetType().GetCustomAttributes(typeof(SDFStateProvided), false))
                {
                    this.providedState.Add(stateProvided.ProvidedType);
                }
            }

            public void RemoveLastProvidedState()
            {
                int index = (int) this.indexStack.Pop();

                this.providedState.RemoveRange(index, providedState.Count - index);
            }

            public bool Contains(Type type)
            {
                return (providedState.Contains(type));
            }
        }

        public static void Load(SDFParsedExpressionList expressionList, SDFState state, ProvidedStatePile parentExpressionStatePile)
        {
            SDFExpressionRegistry expressions = (SDFExpressionRegistry) state[typeof(SDFExpressionRegistry)];
            SDFParsedExpression expression = null;
            SDFTokenStringRegistry tokenStringRegistry = (SDFTokenStringRegistry) state[typeof(SDFTokenStringRegistry)];

            foreach (Object o in expressionList)
            {
                if (o is SDFParsedExpressionList)
                {
                    MaybeCallPostCreateExpression(state, expression, (SDFParsedExpressionList) o);
                    parentExpressionStatePile.AddProvidedStateFromObject(expression.Expression);
                    expression = null;
                    Load((SDFParsedExpressionList) o, state, parentExpressionStatePile);
                    parentExpressionStatePile.RemoveLastProvidedState();
                }
                else
                {
                    if (expression != null)
                    {
                        MaybeCallPostCreateExpression(state, expression, null);
                    }

                    expression = (SDFParsedExpression) o;
                    BindArguments(expression.Arguments, tokenStringRegistry);

                    {
                        object newObject = null;

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

                        {
                            // On the class, see if there are SDFArgument attributes

                            foreach (SDFArgument argument in type.GetCustomAttributes(typeof(SDFArgument), false))
                            {
                                // If the argument is required, then whine if it wasn't specified

                                if ((argument.Required) && (!expression.Arguments.Contains(argument.Name)))
                                {
                                    throw new SDFException(String.Format("Rquired argument '{0}' was not specified", argument.Name));
                                }
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
                                if ((state[stateRequired.RequiredType] == null) && (!parentExpressionStatePile.Contains(stateRequired.RequiredType)))
                                {
                                    throw new SDFException(String.Format("Required state '{0}' was not found", stateRequired.RequiredType.Name));
                                }
                            }
                        }

                        expression.Expression = newObject;
                    }
                }
            }

            if (expression != null)
            {
                MaybeCallPostCreateExpression(state, expression, null);
                expression = null;
            }
        }

        public static void Eval(SDFState state, string eval)
        {
            SDFParsedExpressionList expressionList = SDFExpressionParser.Parse(eval);
            Load(expressionList, state, new ProvidedStatePile());
            expressionList.Evaluate(state);
        }
    }
}
