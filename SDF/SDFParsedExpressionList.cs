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
}
