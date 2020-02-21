using System;
using System.Collections.Generic;
using System.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace DK.LanguageServer
{
	class Span
	{
		private int _start;
		private int _length;

		public Span(int start, int length)
		{
			if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
			if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
			_start = start;
			_length = length;
		}

		public int Start
		{
			get { return _start; }
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException();
				_start = value;
			}
		}

		public int Length
		{
			get { return _length; }
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException();
				_length = value;
			}
		}

		public int End
		{
			get { return _start + _length; }
			set
			{
				if (value < _start) throw new ArgumentOutOfRangeException();
				_length = value - _start;
			}
		}
	}
}
