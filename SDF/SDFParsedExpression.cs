// SDFParsedException.cs created with MonoDevelop
// User: blake at 16:53 12/27/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections;

namespace SDF
{
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
}
