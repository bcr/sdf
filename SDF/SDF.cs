namespace SDF
{
    using NUnit.Framework;

    using System;
    using System.Collections;
    using System.IO;
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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(Foo));

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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooAsFactory));

            SDF.Eval(state, "FooAsFactory");
        }

        public class FooWithRequiredParam
        {
            TokenString argumentVar;

            [SDFArgument]
            public TokenString argument
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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooWithRequiredParam));

            SDF.Eval(this.state, "FooWithRequiredParam");
        }

        [Test]
        public void TestExpressionRequiredParamPresent()
        {
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooWithRequiredParam));

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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooWithRequiredParamClassLevel));

            SDF.Eval(this.state, "FooWithRequiredParamClassLevel");
        }

        [Test]
        public void TestExpressionRequiredParamClassLevelPresent()
        {
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooWithRequiredParamClassLevel));

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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooWithRequiredState));

            SDF.Eval(this.state, "FooWithRequiredState");
        }

        [Test]
        public void TestExpressionRequiredStatePresent()
        {
            this.state += "hear me roar";

            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooWithRequiredState));

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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(StringState));
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FooWithRequiredState));

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
            ((TokenStringRegistry) this.state[typeof(TokenStringRegistry)]).AddType(typeof(upper));

            SDF.Eval(this.state, "Print message='$[upper,foo]'");

            Assert.AreEqual("FOO\n", this.output.ToString());
        }

        private class FixedAnswer
        {
            private TokenString isTrueVar = null;

            [SDFArgument(Required=false)]
            public TokenString isTrue
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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddType(typeof(FixedAnswer));

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
            ((ExpressionRegistry) this.state[typeof(ExpressionRegistry)]).AddObject("ObjectBasedFactoryDude", new ObjectBasedFactory());

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

            AddState(new ExpressionRegistry());
            AddState(new TokenStringRegistry());
        }
    }

    public class SDF
    {
        private SDF()
        {
        }

        private static void MaybeCallPostCreateExpression(SDFState state, ParsedExpression expression, ParsedExpressionList children)
        {
            MethodInfo postCreateMethod = expression.Expression.GetType().GetMethod("PostCreateExpression");
            if (postCreateMethod != null)
            {
                postCreateMethod.Invoke(expression.Expression, new Object[] { state, expression.ExpressionName, expression.Arguments, children });
            }
        }

        private static void BindArguments(Hashtable arguments, TokenStringRegistry tokenStringRegistry)
        {
            Hashtable newArguments = new Hashtable();

            // Not sure if this is stupid or not. I can't modify the collection in place,
            // so I use a copy and then copy it back.

            foreach (string key in arguments.Keys)
            {
                newArguments[key] = TokenString.Parse(tokenStringRegistry, arguments[key].ToString());
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

        public static void Load(ParsedExpressionList expressionList, SDFState state, ProvidedStatePile parentExpressionStatePile)
        {
            ExpressionRegistry expressions = (ExpressionRegistry) state[typeof(ExpressionRegistry)];
            ParsedExpression expression = null;
            TokenStringRegistry tokenStringRegistry = (TokenStringRegistry) state[typeof(TokenStringRegistry)];

            foreach (Object o in expressionList)
            {
                if (o is ParsedExpressionList)
                {
                    MaybeCallPostCreateExpression(state, expression, (ParsedExpressionList) o);
                    parentExpressionStatePile.AddProvidedStateFromObject(expression.Expression);
                    expression = null;
                    Load((ParsedExpressionList) o, state, parentExpressionStatePile);
                    parentExpressionStatePile.RemoveLastProvidedState();
                }
                else
                {
                    if (expression != null)
                    {
                        MaybeCallPostCreateExpression(state, expression, null);
                    }

                    expression = (ParsedExpression) o;
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
            ParsedExpressionList expressionList = ExpressionParser.Parse(eval);
            Load(expressionList, state, new ProvidedStatePile());
            expressionList.Evaluate(state);
        }
    }
}
