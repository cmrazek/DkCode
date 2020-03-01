using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	internal struct DocChar
	{
		private char _ch;
		private int _mainPos;
		private DocPosition _docPos;

		public DocChar(char ch, int mainPosition, DocPosition docPosition)
		{
			_ch = ch;
			_mainPos = mainPosition;
			_docPos = docPosition;
		}

		public char Char => _ch;
		public int MainPosition => _mainPos;
		public DocPosition DocPosition => _docPos;
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
