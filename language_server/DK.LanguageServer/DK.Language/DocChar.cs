using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	[Flags]
	internal enum DocCharFlags
	{
		None = 0x00,
		Visible = 0x01
	}

	internal struct DocChar
	{
		private char _ch;
		private int _mainPos;
		private DocPosition _docPos;
		private DocCharFlags _flags;

		public DocChar(char ch, int mainPosition, DocPosition docPosition, DocCharFlags docCharFlags)
		{
			_ch = ch;
			_mainPos = mainPosition;
			_docPos = docPosition;
			_flags = docCharFlags;
		}

		public char Char => _ch;
		public int MainPosition => _mainPos;
		public DocPosition DocPosition => _docPos;
		public DocCharFlags Flags => _flags;
		public override string ToString() => _ch.ToString();

		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			if (obj.GetType() == typeof(char)) return _ch == (char)obj;
			if (obj.GetType() == typeof(DocChar)) return _ch == ((DocChar)obj)._ch;
			return false;
		}

		public override int GetHashCode()
		{
			return _ch.GetHashCode() ^ _mainPos.GetHashCode() ^ _docPos.GetHashCode();
		}
	}
}
