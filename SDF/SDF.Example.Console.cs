namespace SDF.Example.Console
{
    using System;

    public class ConsoleExample
    {
        public static void Main(string[] args)
        {
            SDF sdf = new SDF();
            SDFState state = new SDFState();

            sdf.Eval(state, Console.In.ReadToEnd());
        }
    }
}
