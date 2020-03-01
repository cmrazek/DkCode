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
		private DocCharFlags[] _flags;
		private Span? _mainSpan;

		public DocString(string text, int[] mainPositions, DocPosition[] docPositions, DocCharFlags[] flags)
		{
			_text = text ?? throw new ArgumentNullException(nameof(text));

			_mainPos = mainPositions ?? throw new ArgumentNullException(nameof(mainPositions));
			if (_mainPos.Length != _text.Length) throw new ArgumentException("Main position array must be the same length as the text");

			_docPos = docPositions ?? throw new ArgumentNullException(nameof(docPositions));
			if (_docPos.Length != _text.Length) throw new ArgumentException("Document position array must be the same length as the text");

			_flags = flags ?? throw new ArgumentNullException(nameof(flags));
			if (_flags.Length != _text.Length) throw new ArgumentException("Document character flags array must be the same length as the text");
		}

		public string Text => _text;
		public DocPosition DocPosition => _docPos.Length > 0 ? _docPos[0] : DocPosition.Empty;
		public override string ToString() => _text;
		public int Length => _text.Length;
		public int[] MainPositions => _mainPos;
		public DocPosition[] DocPositions => _docPos;
		public DocCharFlags[] Flags => _flags;

		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			return _text.Equals(obj.ToString());
		}

		public override int GetHashCode()
		{
			return _text.GetHashCode() ^ _mainPos.GetHashCode() ^ _docPos.GetHashCode() ^ _flags.GetHashCode();
		}

		public Span MainSpan
		{
			get
			{
				if (!_mainSpan.HasValue)
				{
					if (_text.Length == 0) return Span.Empty;

					var last = _text.Length - 1;
					int start = _mainPos[0];
					int end = _flags[last].HasFlag(DocCharFlags.Visible) ? _mainPos[last] + 1 : _mainPos[last];

					_mainSpan = new Span(start, end - start);
				}

				return _mainSpan.Value;
			}
		}
	}
}
