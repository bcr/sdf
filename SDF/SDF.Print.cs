namespace SDF.Print
{
    using System;

    using System.Collections;

    public class Print
    {
        [SDFArgument(Required=true)]
        public string message
        {
            set
            {
            }
        }

        public void Evaluate(SDFState state, string name, Hashtable arguments)
        {
            Console.WriteLine(arguments["message"]);
        }
    }

    public class PrintUpper
    {
        [SDFArgument(Required=true)]
        public string message
        {
            set
            {
            }
        }

        public void Evaluate(SDFState state, string name, Hashtable arguments)
        {
            Console.WriteLine(((string) arguments["message"]).ToUpper());
        }
    }
}
