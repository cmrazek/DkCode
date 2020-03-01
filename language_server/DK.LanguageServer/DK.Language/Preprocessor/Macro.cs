using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language.Preprocessor
{
	class Macro
	{
		public string Name { get; private set; }
		public string[] Arguments { get; private set; }
		public CodeTokenStream Body { get; private set; }

		public Macro(string name, string[] arguments, CodeTokenStream body)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
			Name = name;

			Arguments = arguments;
			if (Arguments != null && Arguments.Length == 0) Arguments = null;

			Body = body;
			if (Body != null && Body.IsEmpty) Body = null;
		}

		public bool HasArguments => Arguments != null;
		public bool HasBody => Body != null;
	}
}
