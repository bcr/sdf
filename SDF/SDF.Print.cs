namespace SDF.Print
{
    using System;

    using System.Collections;

    [SDFArgument(Name="message")]
    public class Print
    {
        public void Evaluate(SDFState state, string name, Hashtable arguments)
        {
            Console.WriteLine(arguments["message"]);
        }
    }

    [SDFArgument(Name="message")]
    public class PrintUpper
    {
        public void Evaluate(SDFState state, string name, Hashtable arguments)
        {
            Console.WriteLine(((string) arguments["message"]).ToUpper());
        }
    }
}
