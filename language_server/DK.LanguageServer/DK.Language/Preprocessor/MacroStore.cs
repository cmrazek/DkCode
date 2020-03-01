using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language.Preprocessor
{
	class MacroStore
	{
		private MacroStore _parent = null;
		private Dictionary<string, Macro> _macros = new Dictionary<string, Macro>();

		public MacroStore()
		{ }

		private MacroStore(MacroStore parent)
		{
			_parent = parent;
		}

		/// <summary>
		/// Adds a macro. Returns true if it already exists; false if it's new.
		/// </summary>
		public bool Add(Macro macro)
		{
			var exists = _macros.ContainsKey(macro.Name);
			_macros[macro.Name] = macro;
			return exists;
		}

		public bool TryGetMacro(string name, out Macro macro)
		{
			if (_macros.TryGetValue(name, out macro)) return true;
			if (_parent != null && _parent.TryGetMacro(name, out macro)) return true;
			return false;
		}

		public MacroStore AddScope()
		{
			return new MacroStore(this);
		}
	}
}
