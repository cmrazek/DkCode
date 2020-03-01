using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	public abstract class BaseCodeReader
	{
		public abstract int Position { get; set; }
		public abstract bool EndOfFile { get; }
		protected abstract void BuildClear();
		protected abstract void Build(int count);
		protected abstract string BuildToString();
		protected abstract CodeToken BuildToToken(CodeType type, bool compiled);
		protected abstract bool BuildExact(char ch);
		protected abstract string BuildWord();
		public abstract char PeekChar();
		public abstract char PeekChar(int offset);
		public abstract string PeekString(int length);
		public abstract void Skip(int count);
		public abstract bool SkipExact(char ch);

		public static bool IsWhiteSpaceChar(char ch) => ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
		public static bool IsCommentStart(string str) => str == "//" || str == "/*";
		public static bool IsEndOfLineChar(char ch) => ch == '\r' || ch == '\n';
		public static bool IsWordChar(char ch, bool firstChar) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_' || (!firstChar && ch >= '0' && ch <= '9');
		public static bool IsDigitChar(char ch) => ch >= '0' && ch <= '9';
		public static bool IsAlphaChar(char ch) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');

		private bool _lastTokenSupportsNegative = true;

		public virtual void ReadAll(CodeTokenStream stream)
		{
			while (!EndOfFile)
			{
				var tok = ReadSimple();
				if (tok.HasValue) stream.Write(tok.Value);
			}
		}

		public CodeToken? ReadSimple(bool stayOnSameLine = false)
		{
			SkipWhite(stayOnSameLine);
			if (EndOfFile) return null;

			BuildClear();

			var ch = PeekChar();
			if (IsWordChar(ch, true))
			{
				Build(1);
				while (IsWordChar(PeekChar(), false)) Build(1);

				switch (BuildToString())
				{
					case "and":	return BuildToToken(CodeType.And, compiled: true);
					case "or":	return BuildToToken(CodeType.Or, compiled: true);
				}

				SkipWhite(stayOnSameLine);

				var chain = false;
				while (PeekChar() == '.')
				{
					Build(1);
					chain = true;
					SkipWhite(stayOnSameLine);

					var first = true;
					while (IsWordChar(PeekChar(), first))
					{
						Build(1);
						first = false;
					}

					SkipWhite(stayOnSameLine);
				}

				return BuildToToken(chain ? CodeType.Chain : CodeType.Word, true);
			}

			if (IsDigitChar(ch))
			{
				while (IsDigitChar(PeekChar())) Build(1);
				if (PeekChar() == '.')
				{
					Build(1);
					while (IsDigitChar(PeekChar())) Build(1);
				}

				_lastTokenSupportsNegative = false;
				return BuildToToken(CodeType.Number, compiled: true);
			}

			if (ch == '\"' || ch == '\'')
			{
				var startCh = ch;
				Build(1);

				while (!EndOfFile)
				{
					ch = PeekChar();
					if (ch == startCh) { Build(1); break; }
					else if (ch == '\\') Build(2);
					else if (IsEndOfLineChar(ch)) break;
					else Build(1);
				}

				_lastTokenSupportsNegative = false;
				return BuildToToken(CodeType.StringLiteral, compiled: true);
			}

			if (ch == '-')
			{
				ch = PeekChar(1);
				if (_lastTokenSupportsNegative && IsDigitChar(ch))
				{
					Build(2);
					while (IsDigitChar(PeekChar())) Build(1);
					if (PeekChar() == '.')
					{
						Build(1);
						while (IsDigitChar(PeekChar())) Build(1);
					}
					_lastTokenSupportsNegative = false;
					return BuildToToken(CodeType.Number, compiled: true);
				}
				else if (ch == '=')
				{
					Build(2);
					_lastTokenSupportsNegative = true;
					return BuildToToken(CodeType.SubtractAssign, compiled: true);
				}
				else
				{
					Build(1);
					_lastTokenSupportsNegative = true;
					return BuildToToken(CodeType.Subtract, compiled: true);
				}
			}

			if (ch == '+' || ch == '*' || ch == '/' || ch == '%' || ch == '=' || ch == '<' || ch == '>')
			{
				if (PeekChar() == '=')
				{
					Build(2);
					var op = string.Concat(ch, "=");
					_lastTokenSupportsNegative = true;
					return BuildToToken(OperatorToCodeType(op), compiled: true);
				}
				else
				{
					Build(1);
					_lastTokenSupportsNegative = true;
					return BuildToToken(OperatorToCodeType(ch.ToString()), compiled: true);
				}
			}

			if (ch == '!')
			{
				if (PeekChar() == '=')
				{
					Build(2);
					_lastTokenSupportsNegative = true;
					return BuildToToken(CodeType.NotEqual, compiled: true);
				}
				else
				{
					Build(1);
					return BuildToToken(CodeType.Invalid, compiled: true);
				}
			}

			if (ch == '(' || ch == '[' || ch == '{' || ch == ',' || ch == '?' || ch == ':' || ch == ';' || ch == '}')
			{
				Build(1);
				_lastTokenSupportsNegative = true;
				return BuildToToken(OperatorToCodeType(ch.ToString()), compiled: true);
			}

			if (ch == ')' || ch == ']')
			{
				Build(1);
				_lastTokenSupportsNegative = false;
				return BuildToToken(OperatorToCodeType(ch.ToString()), compiled: true);
			}

			if (ch == '#')
			{
				Build(1);
				while (IsAlphaChar(PeekChar())) Build(1);
				return BuildToToken(CodeType.Preprocessor, compiled: false);
			}

			Build(1);
			return BuildToToken(CodeType.Invalid, compiled: true);
		}

		public void ReadSimpleNestable(CodeTokenStream stream, CodeType? stopAtType)
		{
			var token = ReadSimple();
			if (!token.HasValue) return;
			stream.Write(token.Value);

			if (stopAtType.HasValue && token.Value.Type == stopAtType.Value) return;

			switch (token.Value.Type)
			{
				case CodeType.OpenBracket:
					ReadSimpleNestable(stream, CodeType.CloseBracket);
					break;
				case CodeType.OpenBrace:
					ReadSimpleNestable(stream, CodeType.CloseBrace);
					break;
				case CodeType.OpenArray:
					ReadSimpleNestable(stream, CodeType.CloseArray);
					break;
			}
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
					var str = PeekString(2);
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
							while (!EndOfFile && PeekChar() != '\n') Skip(1);
						}
					}
					else if (str == "/*")
					{
						// Block comment
						Skip(2);
						var depth = 1;
						while (depth > 0 && !EndOfFile)
						{
							ch = PeekChar();
							if (ch == '/' && PeekString(2) == "/*")
							{
								Skip(2);
								depth++;
							}
							else if (ch == '*' && PeekString(2) == "*/")
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
					else break;
				}
				else break;
			}
		}

		public static CodeType OperatorToCodeType(string op)
		{
			switch (op)
			{
				case "=": return CodeType.Assign;
				case "==": return CodeType.Equal;
				case "!=": return CodeType.NotEqual;
				case "+": return CodeType.Add;
				case "+=": return CodeType.AddAssign;
				case "-": return CodeType.Subtract;
				case "-=": return CodeType.SubtractAssign;
				case "*": return CodeType.Multiply;
				case "*=": return CodeType.MultiplyAssign;
				case "/": return CodeType.Divide;
				case "/=": return CodeType.DivideAssign;
				case "%": return CodeType.Modulus;
				case "%=": return CodeType.ModulusAssign;
				case "<": return CodeType.Less;
				case "<=": return CodeType.LessEqual;
				case ">": return CodeType.Greater;
				case ">=": return CodeType.GreaterEqual;
				case "(": return CodeType.OpenBracket;
				case ")": return CodeType.CloseBracket;
				case "[": return CodeType.OpenArray;
				case "]": return CodeType.CloseArray;
				case "{": return CodeType.OpenBrace;
				case "}": return CodeType.CloseBrace;
				case ",": return CodeType.Comma;
				case "?": return CodeType.Ternary;
				case ":": return CodeType.Colon;
				case ";": return CodeType.Semicolon;
				default: return CodeType.Invalid;
			}
		}

		public string ReadRawTextToEndOfLine()
		{
			BuildClear();
			while (!EndOfFile && !IsEndOfLineChar(PeekChar())) Build(1);
			SkipExact('\r');
			SkipExact('\n');
			return BuildToString();
		}
	}
}
