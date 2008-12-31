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
        private TokenStringRegistry registry = null;
        private SDFState state = null;

        [SetUp]
        public void SetUp()
        {
            this.registry = new TokenStringRegistry();
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

            Assert.AreEqual("FOO", TokenString.Eval(registry, state, "$[upper,foo]"));
        }

        [Test]
        public void TestOneTokenWithSurroundingText()
        {
            registry.AddType(typeof(upper));

            Assert.AreEqual("yap yap, ]yap FOO meow meow, ]meow", TokenString.Eval(registry, state, "yap yap, ]yap $[upper,foo] meow meow, ]meow"));
        }

        [Test]
        public void TestNestedTokens()
        {
            registry.AddType(typeof(upper));
            registry.AddType(typeof(dog));

            Assert.AreEqual("DOGDUDE", TokenString.Eval(registry, state, "$[upper,$[dog]]"));
        }

        [Test]
        [ExpectedException(typeof(SDFException), @"Unknown token 'foo'")]
        public void TestUnknownToken()
        {
            TokenString.Eval(registry, state, "$[foo]");
        }

        public class stringfromstate
        {
            public string Evaluate(SDFState state, ArrayList arguments)
            {
                return (string) state[typeof(string)];
            }
        }

        [Test]
        public void TestStateAccess()
        {
            registry.AddType(typeof(stringfromstate));

            this.state += "stringydingy";

            Assert.AreEqual("stringydingy", TokenString.Eval(registry, state, "$[stringfromstate]"));
        }
    }

    public class TokenString
    {
        private ArrayList elements = new ArrayList();
        private ArrayList arguments = new ArrayList();

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
            get; set;
        }

        private static int ParseTokenString(TokenStringRegistry registry, int offset, string eval, TokenString returnString)
        {
            TokenString currentString = new TokenString();

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

                    TokenString newString = new TokenString();

                    currentString.Add(eval.Substring(offset, tokenStartOffset - offset));

                    offset = ParseTokenString(registry, tokenStartOffset, eval, newString);
                    currentString.Add(newString);
                }
                else if ((commaOffset > -1) && (commaOffset < tokenEndOffset))
                {
                    // If it's a comma, add this run to the current string and start a new string

                    currentString.Add(eval.Substring(offset, commaOffset - offset));
                    returnString.AddArgument(currentString);
                    currentString = new TokenString();
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

        public static TokenString Parse(TokenStringRegistry registry, string eval)
        {
            TokenString newString = new TokenString();
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
                    TokenString innerNewString = new TokenString();

                    newString.Add(eval.Substring(offset, tokenStartOffset - offset));
                    offset = ParseTokenString(registry, tokenStartOffset, eval, innerNewString);
                    newString.Add(innerNewString);
                }
            }

            return newString;
        }

        public static string Eval(TokenStringRegistry registry, SDFState state, string eval)
        {
            return Parse(registry, eval).ToString(state);
        }

        public string ToString(SDFState state)
        {
            StringBuilder returnString = new StringBuilder();

            if (this.arguments.Count > 0)
            {
                MethodInfo method = this.Token.GetType().GetMethod("Evaluate");

                // Call the token with the arguments

                returnString.Append(method.Invoke(this.Token, new Object[] { state, this.arguments }).ToString());
            }
            else
            {
                foreach (object o in this.elements)
                {
                    if (o is TokenString)
                    {
                        returnString.Append(((TokenString) o).ToString(state));
                    }
                    else
                    {
                        returnString.Append(o.ToString());
                    }
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
