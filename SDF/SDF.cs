namespace SDF
{
	using NUnit.Framework;
	using System;
	using System.Collections;
	using System.IO;
	using System.Text.RegularExpressions;
		
	[TestFixture]
	public class TestEval
	{
		private class StandardOutputRedirector
		{
			private StringWriter outputWriter;
			private TextWriter oldWriter;

			public StandardOutputRedirector()
			{
				this.oldWriter = Console.Out;
				this.outputWriter = new StringWriter();
				Console.SetOut(this.outputWriter);
			}

			public void Unhook()
			{
				Console.SetOut(this.oldWriter);
			}
			
			public override string ToString()
			{
				return this.outputWriter.ToString();
			}
		}

		private StandardOutputRedirector output;
		
		[SetUp]
		public void SetUp()
		{
			this.output = new StandardOutputRedirector();
		}

		[TearDown]
		public void TearDown()
		{
			this.output.Unhook();
		}

		[Test]
		public void TestPrint()
		{
			SDF.Eval("Print message='Hello, world'");

			Assert.AreEqual("Hello, world\n", this.output.ToString());
		}

		[Test]
		public void TestPrintUpper()
		{
			SDF.Eval("PrintUpper message='Hello, world'");

			Assert.AreEqual("HELLO, WORLD\n", this.output.ToString());
		}
	}

	public class SDF
	{
		public static void Eval(string eval)
		{
			Regex regex = new Regex(@"\s*(?<expression>\S+)(\s+(?<name>[^ \t=]+)\s*=\s*'(?<value>[^']+)')*");
			Match match = regex.Match(eval);

			string expression = match.Groups["expression"].ToString();
			Group names = match.Groups["name"];
			Group values = match.Groups["value"];

			Hashtable arguments = new Hashtable(names.Captures.Count);

			for (int counter = 0;counter < names.Captures.Count;++counter)
			{
				arguments[names.Captures[counter].ToString()] = values.Captures[counter].ToString();
			}

			switch (expression)
			{
				case "Print":
					Console.WriteLine(arguments["message"]);
					break;
					
				case "PrintUpper":
					Console.WriteLine(((string) arguments["message"]).ToUpper());
					break;
			}
		}
	}
}
