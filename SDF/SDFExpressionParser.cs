// SDFExpressionParser.cs created with MonoDevelop
// User: blake at 16:45 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using NUnit.Framework;

using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace SDF
{
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

    public class SDFExpressionParser
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
}
