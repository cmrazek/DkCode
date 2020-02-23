using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	public class SimpleCodeReader
	{
		private string _code;
		private int _mainStart;
		private DocPosition _docStart;
		private int _pos;
		private int _len;
		private StringBuilder _text = new StringBuilder();
		private bool _lastTokenSupportsNegative = true;

		public SimpleCodeReader(string text, int mainPosition, DocPosition startPosition)
		{
			_code = text ?? throw new ArgumentNullException(nameof(text));
			_mainStart = mainPosition;
			_docStart = startPosition;
			_pos = 0;
			_len = _code.Length;
		}

		public IEnumerable<CodeToken> ReadAll()
		{
			while (!EndOfFile)
			{
				var tok = ReadSimple();
				if (tok.HasValue) yield return tok.Value;
			}
		}

		public CodeToken? ReadSimple()
		{
			SkipWhite();

			if (EndOfFile) return null;

			_text.Clear();

			var mainStart = MainPosition;
			var docStart = DocPosition;
			var ch = PeekChar();
			if (IsWordChar(ch, true))
			{
				_text.Append(ch);
				Skip(1);
				while (IsWordChar(PeekChar(), false)) _text.Append(ReadChar());
				SkipWhite();

				var chain = false;
				while (PeekChar() == '.')
				{
					_text.Append('.');
					Skip(1);
					chain = true;
					SkipWhite();

					var first = true;
					while (IsWordChar(PeekChar(), first))
					{
						_text.Append(ReadChar());
						first = false;
					}

					SkipWhite();
				}

				return new CodeToken(
					text: _text.ToString(),
					type: chain ? CodeType.Chain : CodeType.Word,
					mainSpan: new Span(mainStart, MainPosition - mainStart),
					position: docStart,
					compiled: true);
			}

			if (IsDigitChar(ch))
			{
				while (IsDigitChar(PeekChar())) _text.Append(ReadChar());
				if (PeekChar() == '.')
				{
					_text.Append(ReadChar());
					while (IsDigitChar(PeekChar())) _text.Append(ReadChar());
				}

				_lastTokenSupportsNegative = false;
				return new CodeToken(
					text: _text.ToString(),
					type: CodeType.Number,
					mainSpan: new Span(mainStart, MainPosition - mainStart),
					position: docStart,
					compiled: true);
			}

			if (ch == '\"' || ch == '\'')
			{
				var startCh = ch;
				_text.Append(ch);
				Skip(1);

				while (!EndOfFile)
				{
					ch = PeekChar();
					if (ch == startCh)
					{
						_text.Append(ch);
						Skip(1);
						break;
					}
					else if (ch == '\\') _text.Append(Read(2));
					else if (IsEndOfLineChar(ch)) break;
					else _text.Append(ReadChar());
				}

				_lastTokenSupportsNegative = false;
				return new CodeToken(
					text: _text.ToString(),
					type: CodeType.StringLiteral,
					mainSpan: new Span(mainStart, MainPosition - mainStart),
					position: docStart,
					compiled: true);
			}

			if (ch == '-')
			{
				ch = PeekChar();
				if (_lastTokenSupportsNegative && IsDigitChar(ch))
				{
					_text.Append('-');
					while (IsDigitChar(PeekChar())) _text.Append(ReadChar());
					if (PeekChar() == '.')
					{
						_text.Append(ReadChar());
						while (IsDigitChar(PeekChar())) _text.Append(ReadChar());
					}
					_lastTokenSupportsNegative = false;
					return new CodeToken(
						text: _text.ToString(),
						type: CodeType.Number,
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
				else if (ch == '=')
				{
					Skip(2);
					_lastTokenSupportsNegative = true;
					return new CodeToken(
						text: "-=",
						type: CodeType.SubractAssign,
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
				else
				{
					Skip(1);
					_lastTokenSupportsNegative = true;
					return new CodeToken(
						text: "-",
						type: CodeType.Subtract,
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
			}

			if (ch == '+' || ch == '*' || ch == '/' || ch == '%' || ch == '=')
			{
				if (PeekChar() == '=')
				{
					Skip(2);
					var op = string.Concat(ch, "=");
					_lastTokenSupportsNegative = true;
					return new CodeToken(
						text: op,
						type: PreprocessingCodeReader.OperatorToCodeType(op),
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
				else
				{
					Skip(1);
					_lastTokenSupportsNegative = true;
					return new CodeToken(
						text: ch.ToString(),
						type: PreprocessingCodeReader.OperatorToCodeType(ch.ToString()),
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
			}

			if (ch == '!')
			{
				if (PeekChar() == '=')
				{
					Skip(2);
					_lastTokenSupportsNegative = true;
					return new CodeToken(
						text: "!=",
						type: CodeType.NotEqual,
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
				else
				{
					Skip(1);
					return new CodeToken(
						text: "!",
						type: CodeType.Invalid,
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
			}

			if (ch == '(' || ch == '[' || ch == '{' || ch == ',')
			{
				Skip(1);
				_lastTokenSupportsNegative = true;
				return new CodeToken(
					text: ch.ToString(),
					type: PreprocessingCodeReader.OperatorToCodeType(ch.ToString()),
					mainSpan: new Span(mainStart, MainPosition - mainStart),
					position: docStart,
					compiled: true);
			}

			if (ch == ')' || ch == ']' || ch == '}')
			{
				Skip(1);
				_lastTokenSupportsNegative = false;
				return new CodeToken(
					text: ch.ToString(),
					type: PreprocessingCodeReader.OperatorToCodeType(ch.ToString()),
					mainSpan: new Span(mainStart, MainPosition - mainStart),
					position: docStart,
					compiled: true);
			}

			if (ch == '#')
			{
				_text.Append('#');
				while (IsAlphaChar(PeekChar())) _text.Append(ReadChar());
				return new CodeToken(
					text: _text.ToString(),
					type: CodeType.Preprocessor,
					mainSpan: new Span(mainStart, MainPosition - mainStart),
					position: docStart,
					compiled: false);
			}

			Skip(1);
			return new CodeToken(
				text: ch.ToString(),
				type: CodeType.Invalid,
				mainSpan: new Span(mainStart, MainPosition - mainStart),
				position: docStart,
				compiled: true);
		}

		public void SkipWhite(bool stayOnSameLine = false)
		{
			while (!EndOfFile)
			{
				var ch = PeekChar();
				if (IsWhiteSpaceChar(ch))
				{
					if (stayOnSameLine && IsEndOfLineChar(ch)) break;
					Skip(1);
				}
				else if (ch == '/')
				{
					var str = Peek(2);
					if (str == "//")
					{
						// Line comment
						Skip(2);
						if (stayOnSameLine)
						{
							while (!EndOfFile && !IsEndOfLineChar(PeekChar())) Skip(1);
						}
						else
						{
							while (!EndOfFile && ReadChar() != '\n') ;
						}
					}
					else if (str == "/*")
					{
						// Block comment
						SkipBlockComment();
					}
					else break;
				}
				else break;
			}
		}

		private void SkipBlockComment()
		{
			Skip(2);    // Assumes this method is called while the position is just before '/*'
			var depth = 1;
			while (depth > 0 && !EndOfFile)
			{
				var ch = PeekChar();
				if (ch == '/' && Peek(2) == "/*")
				{
					Skip(2);
					depth++;
				}
				else if (ch == '*' && Peek(2) == "*/")
				{
					Skip(2);
					depth--;
				}
				else
				{
					Skip(1);
				}
			}
		}

		public bool EndOfFile => _pos >= _len;
		public int MainPosition => _pos + _mainStart;
		public DocPosition DocPosition => new DocPosition(_docStart.Document, _docStart.Position + _pos);

		public static bool IsWhiteSpaceChar(char ch) => ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
		public static bool IsCommentStart(string str) => str == "//" || str == "/*";
		public static bool IsEndOfLineChar(char ch) => ch == '\r' || ch == '\n';
		public static bool IsWordChar(char ch, bool firstChar) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_' || (firstChar && ch >= '0' && ch <= '9');
		public static bool IsDigitChar(char ch) => ch >= '0' && ch <= '9';
		public static bool IsAlphaChar(char ch) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');

		public char PeekChar()
		{
			if (_pos >= _len) return '\0';
			return _code[_pos];
		}

		public char ReadChar()
		{
			if (_pos >= _len) throw new EndOfFileException();
			return _code[_pos++];
		}

		public string Peek(int length)
		{
			if (_pos + length >= _len) return _code.Substring(_pos, _len - _pos);
			return _code.Substring(_pos, length);
		}

		public string Read(int length)
		{
			string ret;

			if (_pos + length >= _len)
			{
				ret = _code.Substring(_pos, _len - _pos);
				_pos = _len;
			}
			else
			{
				ret = _code.Substring(_pos, length);
				_pos += length;
			}

			return ret;
		}

		public void Skip(int length)
		{
			_pos += length;
			if (_pos > _len) _pos = _len;
		}
	}
}
