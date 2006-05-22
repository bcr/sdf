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
