using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	class DocString
	{
		private string _text;
		private int[] _mainPos;
		private DocPosition[] _docPos;

		public DocString(string text, int[] mainPositions, DocPosition[] docPositions)
		{
			_text = text ?? throw new ArgumentNullException(nameof(text));

			_mainPos = mainPositions ?? throw new ArgumentNullException(nameof(mainPositions));
			if (_mainPos.Length != _text.Length) throw new ArgumentException("Main position array must be the same length as the text");

			_docPos = docPositions ?? throw new ArgumentNullException(nameof(docPositions));
			if (_docPos.Length != _text.Length) throw new ArgumentException("Document position array must be the same length as the text");
		}

		public string Text => _text;
		public DocPosition DocPosition => _docPos.Length > 0 ? _docPos[0] : DocPosition.Empty;
		public Span MainSpan => _mainPos.Length > 0 ? Span.FromPosition(_mainPos[0], _mainPos[_mainPos.Length - 1] + 1) : Span.Empty;
		public override string ToString() => _text;
		public int Length => _text.Length;
		public int[] MainPositions => _mainPos;
		public DocPosition[] DocPositions => _docPos;

		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			return _text.Equals(obj.ToString());
		}

		public override int GetHashCode()
		{
			return _text.GetHashCode() ^ _mainPos.GetHashCode() ^ _docPos.GetHashCode();
		}
	}
}
