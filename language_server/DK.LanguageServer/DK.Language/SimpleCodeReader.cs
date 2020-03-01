using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	public class SimpleCodeReader : BaseCodeReader
	{
		private string _code;
		private int _mainStart;
		private DocPosition _docStart;
		private int _pos;
		private int _len;
		private StringBuilder _build = new StringBuilder();
		private int _buildPos;

		public SimpleCodeReader(string text, int mainPosition, DocPosition startPosition)
		{
			_code = text ?? throw new ArgumentNullException(nameof(text));
			_mainStart = mainPosition;
			_docStart = startPosition;
			_pos = 0;
			_len = _code.Length;
		}

		public override int Position
		{
			get => _pos;
			set
			{
				if (value < 0 || value > _len) throw new ArgumentOutOfRangeException();
				_pos = value;
			}
		}

		public override bool EndOfFile => _pos >= _len;

		protected override void BuildClear()
		{
			_build.Clear();
			_buildPos = _pos;
		}

		protected override void Build(int count)
		{
			if (_build.Length == 0) _buildPos = _pos;

			if (_pos + count <= _len)
			{
				_build.Append(_code.Substring(_pos, count));
				_pos += count;
			}
			else if (_pos < _len)
			{
				_build.Append(_code.Substring(_pos, _len - _pos));
				_pos = _len;
			}
		}

		protected override string BuildToString()
		{
			return _build.ToString();
		}

		protected override CodeToken BuildToToken(CodeType type, bool compiled)
		{
			return new CodeToken(_build.ToString(), type, new Span(_buildPos, _build.Length), _docStart + _buildPos, compiled);
		}

		protected override string BuildWord()
		{
			_build.Clear();
			var first = true;
			while (_pos < _len && IsWordChar(_code[_pos], first))
			{
				if (first) _buildPos = _pos;
				else first = false;
				_build.Append(_code[_pos++]);
			}
			return _build.ToString();
		}

		protected override bool BuildExact(char ch)
		{
			if (_pos < _len && _code[_pos] == ch)
			{
				_build.Clear();
				_build.Append(_code[_pos]);
				_buildPos = _pos++;
				return true;
			}
			return false;
		}

		public override char PeekChar()
		{
			if (_pos >= _len) return '\0';
			return _code[_pos];
		}

		public override char PeekChar(int offset)
		{
			if (_pos + offset < 0 || _pos + offset >= _len) return '\0';
			return _code[_pos + offset];
		}

		public override string PeekString(int length)
		{
			if (_pos + length <= _len) return _code.Substring(_pos, length);
			return _code.Substring(_pos, _len - _pos);
		}

		public override void Skip(int count)
		{
			_pos += count;
			if (_pos > _len) _pos = _len;
		}

		public override bool SkipExact(char ch)
		{
			if (_pos < _len && _code[_pos] == ch)
			{
				_pos++;
				return true;
			}
			return false;
		}
	}
}
