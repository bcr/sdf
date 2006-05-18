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
		private class Print
		{
		    public void Evaluate(Hashtable arguments)
		    {
		        Console.WriteLine(arguments["message"]);
		    }
		}

		private class PrintUpper
		{
		    public void Evaluate(Hashtable arguments)
		    {
				Console.WriteLine(((string) arguments["message"]).ToUpper());
		    }
		}

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
		private SDF sdf;
		
		[SetUp]
		public void SetUp()
		{
			this.output = new StandardOutputRedirector();
			this.sdf = new SDF();
			{
				Type[] types = { typeof(Print), typeof(PrintUpper) };
				foreach (Type type in types)
				{
					this.sdf.AddType(type);
				}
			}
		}

		[TearDown]
		public void TearDown()
		{
			this.output.Unhook();
		}

		[Test]
		public void TestPrint()
		{
			this.sdf.Eval("Print message='Hello, world'");

			Assert.AreEqual("Hello, world\n", this.output.ToString());
		}

		[Test]
		public void TestPrintUpper()
		{
			this.sdf.Eval("PrintUpper message='Hello, world'");

			Assert.AreEqual("HELLO, WORLD\n", this.output.ToString());
		}
	}

	public class SDF
	{
		private Hashtable expressions = new Hashtable();

		public void AddType(Type type)
		{
			expressions[type.Name] = type;
		}

		public void Eval(string eval)
		{
			string expression = null;
			Hashtable arguments = null;
			
			{
				Regex regex = new Regex(@"\s*(?<expression>\S+)(\s+(?<name>[^ \t=]+)\s*=\s*'(?<value>[^']+)')*");
				Match match = regex.Match(eval);

				Group names = match.Groups["name"];
				Group values = match.Groups["value"];

				expression = match.Groups["expression"].ToString();
				arguments = new Hashtable(names.Captures.Count);

				for (int counter = 0;counter < names.Captures.Count;++counter)
				{
					arguments[names.Captures[counter].ToString()] = values.Captures[counter].ToString();
				}
			}

			{
				Type type = (Type) expressions[expression];
				object o = type.GetConstructor(new Type[0]).Invoke(null);
				type.GetMethod("Evaluate").Invoke(o, new Object[] { arguments });
			}
		}
	}
}
