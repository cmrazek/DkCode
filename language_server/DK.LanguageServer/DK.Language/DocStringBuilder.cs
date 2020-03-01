using System;
using System.Collections.Generic;
using System.Text;
using DK.Common;

namespace DK.Language
{
	internal class DocStringBuilder
	{
		private StringBuilder _text = new StringBuilder();
		private List<int> _mainPos = new List<int>();
		private List<DocPosition> _docPos = new List<DocPosition>();
		private int _len;

		public DocStringBuilder()
		{
		}

		public DocString DocText => new DocString(_text.ToString(), _mainPos.ToArray(), _docPos.ToArray());
		public int Length => _len;
		public DocPosition StartPosition => _docPos.Count > 0 ? _docPos[0] : DocPosition.Empty;
		public override string ToString() => _text.ToString();

		public void Clear()
		{
			_text.Clear();
			_mainPos.Clear();
			_docPos.Clear();
			_len = 0;
		}

		public void Append(string text, int mainPosition, DocPosition docPosition, bool isMainContent)
		{
			_text.Append(text);
			var len = text.Length;

			if (isMainContent)
			{
				for (int i = 0; i < len; i++) _mainPos.Add(i);
			}
			else
			{
				var lastMainPos = _len > 0 ? _mainPos[_len - 1] : 0;
				for (int i = 0; i < len; i++) _mainPos.Add(lastMainPos);
			}

			for (int i = 0; i < len; i++)
			{
				_docPos.Add(docPosition);
				docPosition += 1;
			}

			_len += len;
		}

		public void Append(DocString str)
		{
			if (str.Length > 0)
			{
				_text.Append(str.Text);
				_mainPos.AddRange(str.MainPositions);
				_docPos.AddRange(str.DocPositions);
			}
		}

		public void Append(DocChar ch)
		{
			_text.Append(ch.Char);
			_mainPos.Add(ch.MainPosition);
			_docPos.Add(ch.DocPosition);
		}

		public void InsertDoc(int index, string text, Document doc)
		{
			if (index < 0 || index > _len) throw new ArgumentOutOfRangeException(nameof(index));

			if (index == _len)
			{
				_text.Append(text);
				var lastMainPos = _len > 0 ? _mainPos[_len - 1] : 0;
				for (int i = 0, ii = text.Length; i < ii; i++) _mainPos.Add(lastMainPos);
				for (int i = 0, ii = text.Length; i < ii; i++) _docPos.Add(new DocPosition(doc, i));
				_len += text.Length;
			}
			else
			{
				_text.Insert(index, text);

				var mainArr = new int[text.Length];
				var mainPos = _mainPos[index];
				for (int i = 0, ii = text.Length; i < ii; i++) mainArr[i] = mainPos;
				_mainPos.InsertRange(index, mainArr);

				var docArr = new DocPosition[text.Length];
				for (int i = 0, ii = text.Length; i < ii; i++) docArr[i] = new DocPosition(doc, i);
				_docPos.InsertRange(index, docArr);

				_len += text.Length;
			}
		}

		public DocChar this[int index]
		{
			get
			{
				if (index < 0 || index >= _len) throw new ArgumentOutOfRangeException(nameof(index));
				return new DocChar(_text[index], _mainPos[index], _docPos[index]);
			}
		}

		public int GetMainPosition(int index)
		{
			if (index < 0 || index > _len) throw new ArgumentOutOfRangeException(nameof(index));
			if (index < _len) return _mainPos[index];
			return _mainPos[_len - 1];
		}

		public DocString Substring(int start, int length)
		{
			if (start < 0 || start > _len) throw new ArgumentOutOfRangeException(nameof(start));
			if (length < 0 || start + length > _len) throw new ArgumentOutOfRangeException(nameof(length));

			return new DocString(_text.ToString(start, length), _mainPos.GetRange(start, length).ToArray(), _docPos.GetRange(start, length).ToArray());
		}

		public DocString Substring(int start)
		{
			if (start < 0 || start > _len) throw new ArgumentOutOfRangeException(nameof(start));

			var length = _len - start;
			return new DocString(_text.ToString(start, length), _mainPos.GetRange(start, length).ToArray(), _docPos.GetRange(start, length).ToArray());
		}

		public DocString ToDocString()
		{
			return new DocString(_text.ToString(), _mainPos.ToArray(), _docPos.ToArray());
		}
	}
}
