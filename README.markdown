Welcome to the Simple Declarative Framework
===========================================

The Git repository is hosted on [GitHub](http://github.com/bcr/sdf)

The Simple Declarative Framework (SDF -- type it -- it's fun!) is meant to be a minimalist framework for implementing domain specific languages. The syntax is very simple. The regex used for the actual parsing is as follows:

    Regex regex = new Regex(@"\n?(?<indent>[ \t]*)(?<expression>\S+)(\s+(?<name>[^ \t=]+)\s*=\s*'(?<value>[^']+)')*");

No making fun of the regex and its problems, but basically the fundamentals are:

* An SDF source stream is a collection of expressions.
* An expression takes a set of zero or more uniquely named arguments.
* Expressions operate on an SDFState object.
* Expressions can have subexpressions.
* An expression's parent/child/sibling relationships are specified by the whitespace indentation level of its name.
* An expression can produce state which is added to the SDFState object. This state is visible to the children of the expression, but not to the siblings or parent.
* Subexpressions are executed when the parent expression produces state.

Got it? The following is actual sample code distributed with SDF:

    namespace SDF.Example.Console
    {
        using System;

        public class ConsoleExample
        {
            public static void Main(string[] args)
            {
                SDF.Eval(new SDFState(), Console.In.ReadToEnd());
            }
        }
    }

Man, that sure was a lot of code. When provided with the following input, magic things happen:

    LoadExpressions filename='SDF.Print.dll'

    Expression name='Foo'
            Print message='This is Foo'

    Expression name='Bar'
            Print message='This is Bar'

    Bar
    Foo
    Foo
    Bar

The output of this is:

    ~$ mono SDF.Example.Console.exe < example.sdf
    This is Bar
    This is Foo
    This is Foo
    This is Bar

Let's take a look at this step-by-step:

    LoadExpressions filename='SDF.Print.dll'

This finds an assembly called "SDF.Print.dll" and loads and defines all of the expressions in it. This assembly defines the Print expression. The LoadExpressions expression is one of two predefined expressions in the SDF core.

    Expression name='Foo'
            Print message='This is Foo'

This defines a custom expression called "Foo" that will print the message "This is Foo" when it is called. It uses the Print expression defined in SDF.Print.dll. The body of this expression is not executed automatically, only when the Foo expression is called.

    Expression name='Bar'
            Print message='This is Bar'

Same thing as Foo only it's Bar.

    Bar
    Foo
    Foo
    Bar

These invoke the two custom expressions. They are expressions with zero arguments.

The implementation of the Print expression in SDF.Print.dll is as follows:

    [SDFArgument(Name="message")]
    public class Print
    {
        public void Evaluate(SDFState state, string name, Hashtable arguments)
        {
            Console.WriteLine(arguments["message"]);
        }
    }

The SDFArgument attribute is used to mark the argument messsage as required.
The Evaluate method is invoked when the expression is executed.