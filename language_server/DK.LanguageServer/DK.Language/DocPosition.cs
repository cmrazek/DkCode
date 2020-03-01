using System;
using System.Collections.Generic;
using System.Text;
using DK.Common;

namespace DK.Language
{
	public struct DocPosition
	{
		private Document _doc;
		private int _pos;

		public static readonly DocPosition Empty = new DocPosition(null, 0);

		public DocPosition(Document document, int position)
		{
			_doc = document;
			_pos = position;
			if (_pos < 0) throw new ArgumentOutOfRangeException(nameof(position));
		}

		public Document Document => _doc;
		public int Position => _pos;

		public override string ToString() => $"[{_doc.Uri} Offset:{_pos}]";

		public static DocPosition operator +(DocPosition docPos, int offset)
		{
			return new DocPosition(docPos._doc, docPos._pos + offset);
		}

		public static DocPosition operator -(DocPosition docPos, int offset)
		{
			return new DocPosition(docPos._doc, docPos._pos + offset);
		}
	}
}
