using System;
using System.Linq;
using DK.Common;
using DK.Language;

namespace DK.Test
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Environment.ExitCode = new Program().Run(args);
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.ToString());
				Console.ResetColor();
				Environment.ExitCode = 1;
			}
		}

		private int ShowUsage(string message)
		{
			if (!string.IsNullOrEmpty(message))
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(message);
				Console.ResetColor();
				Console.WriteLine();
			}

			Console.WriteLine("Usage:");
			Console.WriteLine("  DK.Test.exe <command>");
			Console.WriteLine();
			Console.WriteLine("Commands:");
			Console.WriteLine("  preprocess <fileName> <outputReport>");
			Console.WriteLine();

			return 1;
		}

		private int Run(string[] args)
		{
			if (args.Length == 0) return ShowUsage(null);

			var command = args[0];
			args = args.Skip(1).ToArray();
			switch (command.ToLower())
			{
				case "preprocess":
					if (args.Length != 2) return ShowUsage("Expected file name after 'preprocess'.");
					DoPreprocess(args[0], args[1]);
					return 0;

				default:
					return ShowUsage($"Invalid command '{command}'.");
			}
		}

		private void DoPreprocess(string fileName, string outputReport)
		{
			var profile = new DkProfile();
			var rdr = new PreprocessingCodeReader(profile, new Uri(fileName), null);

			using (var rep = new System.IO.StreamWriter(outputReport))
			{
				rdr.ReadAll(new CodeTokenListener(token =>
				{
					rep.WriteLine(token.ToString());
				}));
			}
		}
	}
}
