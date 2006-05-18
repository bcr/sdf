namespace SDF.Print
{
	using System;

	using System.Collections;

	public class Print
	{
	    public void Evaluate(SDF sdf, Hashtable arguments)
	    {
	        Console.WriteLine(arguments["message"]);
	    }
	}

	public class PrintUpper
	{
	    public void Evaluate(SDF sdf, Hashtable arguments)
	    {
			Console.WriteLine(((string) arguments["message"]).ToUpper());
	    }
	}
}
