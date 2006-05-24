namespace SDF
{
    using NUnit.Framework;
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Text;

    [TestFixture]
    public class TestSDFTokenString
    {
        private SDFTokenStringRegistry registry = null;
        private SDFState state = null;

        [SetUp]
        public void SetUp()
        {
            this.registry = new SDFTokenStringRegistry();
            this.state = new SDFState();
        }

        public class upper
        {
            public string Evaluate(SDFState state, ArrayList arguments)
            {
                return arguments[1].ToString().ToUpper();
            }
        }

        public class dog
        {
            public string Evaluate(SDFState state, ArrayList arguments)
            {
                return "dogdude";
            }
        }

        [Test]
        public void TestOneToken()
        {
            registry.AddType(typeof(upper));

            Assert.AreEqual("FOO", SDFTokenString.Eval(registry, state, "$[upper,foo]"));
        }

        [Test]
        public void TestOneTokenWithSurroundingText()
        {
            registry.AddType(typeof(upper));

            Assert.AreEqual("yap yap, ]yap FOO meow meow, ]meow", SDFTokenString.Eval(registry, state, "yap yap, ]yap $[upper,foo] meow meow, ]meow"));
        }

        [Test]
        public void TestNestedTokens()
        {
            registry.AddType(typeof(upper));
            registry.AddType(typeof(dog));

            Assert.AreEqual("DOGDUDE", SDFTokenString.Eval(registry, state, "$[upper,$[dog]]"));
        }

        [Test]
        [ExpectedException(typeof(SDFException), @"Unknown token 'foo'")]
        public void TestUnknownToken()
        {
            SDFTokenString.Eval(registry, state, "$[foo]");
        }
    }

    public class SDFTokenStringRegistry
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
            Type type = (Type) this[name];

            if (type == null)
            {
                throw new SDFException(String.Format("Unknown token '{0}'", name));
            }

            return type.GetConstructor(new Type[0]).Invoke(null);
        }

        public SDFTokenStringRegistry()
        {
        }
    }

    public class SDFTokenString
    {
        private ArrayList elements = new ArrayList();
        private ArrayList arguments = new ArrayList();
        private object token = null;

        static private readonly string TOKEN_START = "$[";
        static private readonly string TOKEN_END = "]";

        enum PARSE_STATE { PARSE_REGULAR_CHARS = 0, PARSE_TOKEN_NAME, PARSE_TOKEN_PARAM };

        public string Name
        {
            get
            {
                return arguments[0].ToString();
            }
        }

        public object Token
        {
            set
            {
                this.token = value;
            }
        }

        private static int ParseTokenString(SDFTokenStringRegistry registry, int offset, string eval, SDFTokenString returnString)
        {
            SDFTokenString currentString = new SDFTokenString();

            // Eat the TOKEN_START

            offset += TOKEN_START.Length;

            while (offset < eval.Length)
            {
                // Find the next TOKEN_START, comma, or TOKEN_END

                int tokenStartOffset = eval.IndexOf(TOKEN_START, offset);
                int commaOffset = eval.IndexOf(',', offset);
                int tokenEndOffset = eval.IndexOf(TOKEN_END, offset);

                if (tokenStartOffset > -1)
                {
                    commaOffset = (commaOffset == -1) ? eval.Length : commaOffset;
                    tokenEndOffset = (tokenEndOffset == -1) ? eval.Length : tokenEndOffset;
                }
                else if (commaOffset > -1)
                {
                    tokenEndOffset = (tokenEndOffset == -1) ? eval.Length : tokenEndOffset;
                }

                if ((tokenStartOffset > -1) && ((tokenStartOffset < commaOffset) && (tokenStartOffset < tokenEndOffset)))
                {
                    // If it's TOKEN_START, add this run to the current string, and call recursively

                    SDFTokenString newString = new SDFTokenString();

                    currentString.Add(eval.Substring(offset, tokenStartOffset - offset));

                    offset = ParseTokenString(registry, tokenStartOffset, eval, newString);
                    currentString.Add(newString);
                }
                else if ((commaOffset > -1) && (commaOffset < tokenEndOffset))
                {
                    // If it's a comma, add this run to the current string and start a new string

                    currentString.Add(eval.Substring(offset, commaOffset - offset));
                    returnString.AddArgument(currentString);
                    currentString = new SDFTokenString();
                    offset = commaOffset + 1;
                }
                else
                {
                    // If it's a TOKEN_END, add this run to the current string and return

                    currentString.Add(eval.Substring(offset, tokenEndOffset - offset));
                    returnString.AddArgument(currentString);
                    returnString.Token = registry.NewObject(returnString.Name);
                    return tokenEndOffset + 1;
                }
            }

            return offset;
        }

        public void Add(object o)
        {
            this.elements.Add(o);
        }

        public void AddArgument(object o)
        {
            this.arguments.Add(o);
        }

        public static string Eval(SDFTokenStringRegistry registry, SDFState state, string eval)
        {
            SDFTokenString newString = new SDFTokenString();
            int offset = 0;

            while (offset < eval.Length)
            {
                int tokenStartOffset = eval.IndexOf(TOKEN_START, offset);

                if (tokenStartOffset == -1)
                {
                    newString.Add(eval.Substring(offset, eval.Length - offset));
                    offset = eval.Length;
                }
                else
                {
                    SDFTokenString innerNewString = new SDFTokenString();

                    newString.Add(eval.Substring(offset, tokenStartOffset - offset));
                    offset = ParseTokenString(registry, tokenStartOffset, eval, innerNewString);
                    newString.Add(innerNewString);
                }
            }

            return newString.ToString(state);
        }

        public string ToString(SDFState state)
        {
            StringBuilder returnString = new StringBuilder();

            if (this.arguments.Count > 0)
            {
                MethodInfo method = this.token.GetType().GetMethod("Evaluate");

                // Call the token with the arguments

                returnString.Append(method.Invoke(this.token, new Object[] { state, this.arguments }).ToString());
            }
            else
            {
                foreach (object o in this.elements)
                {
                    returnString.Append(o.ToString());
                }
            }

            return returnString.ToString();
        }

        public override string ToString()
        {
            return ToString(new SDFState());
        }
    }
}
