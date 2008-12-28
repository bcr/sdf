// SDFParsedExpressionList.cs created with MonoDevelop
// User: blake at 16:47 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;
using System.Reflection;

namespace SDF
{
    public class ParsedExpressionList : IEnumerable
    {
        private ArrayList expressionList = new ArrayList();

        public int IndentLevel
        {
            get
            {
                return ((ParsedExpression) expressionList[0]).IndentLevel;
            }
        }

        public static ParsedExpressionList operator+(ParsedExpressionList list, ParsedExpression parsedExpression)
        {
            list.AddExpression(parsedExpression);
            return list;
        }

        public static ParsedExpressionList operator+(ParsedExpressionList list, ParsedExpressionList parsedExpressionList)
        {
            list.AddExpressionList(parsedExpressionList);
            return list;
        }

        public void AddExpression(ParsedExpression parsedExpression)
        {
            this.expressionList.Add(parsedExpression);
        }

        public void AddExpressionList(ParsedExpressionList parsedExpressionList)
        {
            this.expressionList.Add(parsedExpressionList);
        }

        public IEnumerator GetEnumerator()
        {
            return this.expressionList.GetEnumerator();
        }

        public static void Evaluate(ParsedExpressionList expressionList, SDFState state)
        {
            Object lastExpressionObject = null;

            foreach (Object o in expressionList)
            {
                if (o is ParsedExpressionList)
                {
                    if (lastExpressionObject != null)
                    {
                        state += lastExpressionObject;
                        try
                        {
                            Evaluate((ParsedExpressionList) o, state);
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
                    ParsedExpression expression = (ParsedExpression) o;
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
}
