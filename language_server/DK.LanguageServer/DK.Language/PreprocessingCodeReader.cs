using System;
using System.Collections.Generic;
using System.Text;
using DK.Common;

namespace DK.Language
{
	/// <summary>
	/// Reads code while applying preprocessing inline as it's encountered
	/// </summary>
	public class PreprocessingCodeReader
	{
		private DkProfile _profile;
		private Uri _uri;
		private Document _doc;
		private List<CodeSegment> _segments = new List<CodeSegment>();
		private int _mainLength;
		private bool _lastTokenSupportsNegative = true;

		public PreprocessingCodeReader(DkProfile profile, Uri uri, Document document)
		{
			_profile = profile ?? throw new ArgumentNullException(nameof(profile));
			_uri = uri ?? throw new ArgumentNullException(nameof(uri));

			if (document != null)
			{
				if (document.Text.Length > 0) _segments.Add(new CodeSegment(document, 0, document.Text, 0, 0));
				_mainLength = document.Text.Length;
				_doc = document;
			}
			else
			{
				_doc = profile.GetDocument(uri);
				if (_doc.Text.Length > 0) _segments.Add(new CodeSegment(_doc, 0, _doc.Text, 0, 0));
				_mainLength = _doc.Text.Length;
			}
		}

		#region Reading Methods
		// The following properties get set for each read
		private StringBuilder _text = new StringBuilder();

		private CodeToken? ReadSimple(CodeSegment withinSegment)
		{
			SkipWhite(withinSegment);

			if (EndOfFile(withinSegment)) return null;

			_text.Clear();

			var mainStart = MainPosition;
			var docStart = DocPosition;
			var ch = PeekChar(withinSegment);
			if (IsWordChar(ch, true))
			{
				_text.Append(ch);
				Skip(1);
				while (IsWordChar(PeekChar(withinSegment), false)) _text.Append(ReadChar(withinSegment));
				SkipWhite(withinSegment);

				var chain = false;
				while (PeekChar(withinSegment) == '.')
				{
					_text.Append('.');
					Skip(1);
					chain = true;
					SkipWhite(withinSegment);

					var first = true;
					while (IsWordChar(PeekChar(withinSegment), first))
					{
						_text.Append(ReadChar(withinSegment));
						first = false;
					}

					SkipWhite(withinSegment);
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
				while (IsDigitChar(PeekChar(withinSegment))) _text.Append(ReadChar(withinSegment));
				if (PeekChar(withinSegment) == '.')
				{
					_text.Append(ReadChar(withinSegment));
					while (IsDigitChar(PeekChar(withinSegment))) _text.Append(ReadChar(withinSegment));
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

				while (!EndOfFile(withinSegment))
				{
					ch = PeekChar(withinSegment);
					if (ch == startCh)
					{
						_text.Append(ch);
						Skip(1);
						break;
					}
					else if (ch == '\\') _text.Append(Read(2, withinSegment));
					else if (IsEndOfLineChar(ch)) break;
					else _text.Append(ReadChar(withinSegment));
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
				ch = PeekChar(withinSegment);
				if (_lastTokenSupportsNegative && IsDigitChar(ch))
				{
					_text.Append('-');
					while (IsDigitChar(PeekChar(withinSegment))) _text.Append(ReadChar(withinSegment));
					if (PeekChar(withinSegment) == '.')
					{
						_text.Append(ReadChar(withinSegment));
						while (IsDigitChar(PeekChar(withinSegment))) _text.Append(ReadChar(withinSegment));
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
				if (PeekChar(withinSegment) == '=')
				{
					Skip(2);
					var op = string.Concat(ch, "=");
					_lastTokenSupportsNegative = true;
					return new CodeToken(
						text: op,
						type: OperatorToCodeType(op),
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
						type: OperatorToCodeType(ch.ToString()),
						mainSpan: new Span(mainStart, MainPosition - mainStart),
						position: docStart,
						compiled: true);
				}
			}

			if (ch == '!')
			{
				if (PeekChar(withinSegment) == '=')
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
					type: OperatorToCodeType(ch.ToString()),
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
					type: OperatorToCodeType(ch.ToString()),
					mainSpan: new Span(mainStart, MainPosition - mainStart),
					position: docStart,
					compiled: true);
			}

			if (ch == '#')
			{
				_text.Append('#');
				Skip(1);
				while (IsAlphaChar(PeekChar(withinSegment))) _text.Append(ReadChar(withinSegment));
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

		private IEnumerable<CodeToken> ReadSimpleNestable(CodeSegment withinSegment, CodeType? stopAtType)
		{
			var token = ReadSimple(withinSegment);
			if (!token.HasValue) yield break;

			yield return token.Value;

			if (stopAtType.HasValue && token.Value.Type == stopAtType.Value) yield break;

			switch (token.Value.Type)
			{
				case CodeType.OpenBracket:
					foreach (var t in ReadSimpleNestable(withinSegment, CodeType.CloseBracket)) yield return t;
					break;
				case CodeType.OpenBrace:
					foreach (var t in ReadSimpleNestable(withinSegment, CodeType.CloseBrace)) yield return t;
					break;
				case CodeType.OpenArray:
					foreach (var t in ReadSimpleNestable(withinSegment, CodeType.CloseArray)) yield return t;
					break;
			}
		}

		public IEnumerable<CodeToken> ReadAll()
		{
			SkipWhite(null);

			while (!EndOfFile(null))
			{
				var tokenSegment = CurrentSegment;
				var token = ReadSimple(null);
				if (!token.HasValue) continue;

				if (token.Value.Type == CodeType.Preprocessor)
				{
					foreach (var t in ReadPreprocessor(token.Value, tokenSegment)) yield return t;
				}
				else
				{
					yield return token.Value;
				}
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
				case "-=": return CodeType.SubractAssign;
				case "*": return CodeType.Multiply;
				case "*=": return CodeType.MultiplyAssign;
				case "/": return CodeType.Divide;
				case "/=": return CodeType.DivideAssign;
				case "%": return CodeType.Modulus;
				case "%=": return CodeType.ModulusAssign;
				case "(": return CodeType.OpenBracket;
				case ")": return CodeType.CloseBracket;
				case "[": return CodeType.OpenArray;
				case "]": return CodeType.CloseArray;
				case "{": return CodeType.OpenBrace;
				case "}": return CodeType.CloseBrace;
				case ",": return CodeType.Comma;
				default: return CodeType.Invalid;
			}
		}

		private char ReadChar(CodeSegment withinSegment)
		{
			if (_segments.Count == 0) throw new EndOfFileException();
			if (withinSegment != null && !_segments.Contains(withinSegment)) return '\0';

			var seg = _segments[_segments.Count - 1];
			var ch = seg.Code[seg.Position++];
			if (seg.Position >= seg.Code.Length)
			{
				_segments.Remove(seg);
				if (_segments.Count > 0) _segments[_segments.Count - 1].Position = seg.ReturnOffset;
			}
			return ch;
		}

		private char PeekChar(CodeSegment withinSegment)
		{
			if (_segments.Count == 0) return '\0';
			if (withinSegment != null && !_segments.Contains(withinSegment)) return '\0';

			var seg = _segments[_segments.Count - 1];
			return seg.Code[seg.Position];
		}

		private int Read(int length, CodeSegment withinSegment)
		{
			if (_segments.Count == 0 || length == 0) return 0;

			var remain = length;
			var skipped = 0;

			while (remain > 0 && (withinSegment == null || _segments.Contains(withinSegment)))
			{
				var seg = _segments[_segments.Count - 1];
				var available = seg.Code.Length - seg.Position;
				if (available > remain)
				{
					_text.Append(seg.Code.Substring(seg.Position, remain));
					seg.Position += remain;
					skipped += remain;
					break;
				}
				else
				{
					_text.Append(seg.Code.Substring(seg.Position, available));
					remain -= available;
					skipped += available;
					_segments.Remove(seg);
					if (_segments.Count > 0) _segments[_segments.Count - 1].Position = seg.ReturnOffset;
				}
			}

			return skipped;
		}

		private string Peek(int length, CodeSegment withinSegment)
		{
			if (_segments.Count == 0 || length == 0) return string.Empty;

			var ret = string.Empty;
			var remain = length;
			var segIndex = _segments.Count - 1;
			var offset = _segments[segIndex].Position;

			while (remain > 0 && (withinSegment == null || _segments.Contains(withinSegment)))
			{
				var seg = _segments[segIndex];
				var available = seg.Code.Length - offset;
				if (available > remain)
				{
					ret += seg.Code.Substring(offset, remain);
					break;
				}
				else
				{
					ret += seg.Code.Substring(offset, available);
					remain -= available;
					offset = seg.ReturnOffset;
					if (segIndex-- == 0) break;
				}
			}

			return ret;
		}

		private int Skip(int length)
		{
			if (_segments.Count == 0 || length == 0) return 0;

			var remain = length;
			var skipped = 0;

			while (remain > 0)
			{
				var seg = _segments[_segments.Count - 1];
				var available = seg.Code.Length - seg.Position;
				if (available > remain)
				{
					seg.Position += remain;
					skipped += remain;
					break;
				}
				else
				{
					remain -= available;
					skipped += available;
					_segments.Remove(seg);
					if (_segments.Count > 0) _segments[_segments.Count - 1].Position = seg.ReturnOffset;
				}
			}

			return skipped;
		}

		private bool ReadExact(char ch, CodeSegment withinSegment)
		{
			if (PeekChar(withinSegment) == ch)
			{
				Skip(1);
				return true;
			}

			return false;
		}

		private bool PeekExact(char ch, CodeSegment withinSegment)
		{
			return PeekChar(withinSegment) == ch;
		}

		private string ReadWord(CodeSegment withinSegment)
		{
			_text.Clear();
			var first = true;
			while (IsWordChar(PeekChar(withinSegment), first))
			{
				_text.Append(ReadChar(withinSegment));
				first = false;
			}
			return _text.ToString();
		}

		private string ReadRawTextToEndOfLine(CodeSegment withinSegment)
		{
			_text.Clear();
			while (!IsEndOfLineChar(PeekChar(withinSegment)) && (withinSegment == null || _segments.Contains(withinSegment)))
			{
				_text.Append(ReadChar(withinSegment));
			}
			ReadExact('\r', withinSegment);
			ReadExact('\n', withinSegment);
			return _text.ToString();
		}

		private bool EndOfFile(CodeSegment withinSegment)
		{
			if (withinSegment == null) return _segments.Count == 0;
			return !_segments.Contains(withinSegment);
		}

		public static bool IsWhiteSpaceChar(char ch) => ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
		public static bool IsCommentStart(string str) => str == "//" || str == "/*";
		public static bool IsEndOfLineChar(char ch) => ch == '\r' || ch == '\n';
		public static bool IsWordChar(char ch, bool firstChar) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == '_' || (firstChar && ch >= '0' && ch <= '9');
		public static bool IsDigitChar(char ch) => ch >= '0' && ch <= '9';
		public static bool IsAlphaChar(char ch) => (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');

		private void SkipWhite(CodeSegment withinSegment, bool stayOnSameLine = false)
		{
			while (!EndOfFile(withinSegment))
			{
				var ch = PeekChar(withinSegment);
				if (IsWhiteSpaceChar(ch))
				{
					if (stayOnSameLine && IsEndOfLineChar(ch)) break;
					Skip(1);
				}
				else if (ch == '/')
				{
					var str = Peek(2, withinSegment);
					if (str == "//")
					{
						// Line comment
						Skip(2);
						if (stayOnSameLine)
						{
							while (!EndOfFile(withinSegment) && !IsEndOfLineChar(PeekChar(withinSegment))) Skip(1);
						}
						else
						{
							while (!EndOfFile(withinSegment) && ReadChar(withinSegment) != '\n') ;
						}
					}
					else if (str == "/*")
					{
						// Block comment
						SkipBlockComment(withinSegment);
					}
					else break;
				}
				else break;
			}
		}

		private void SkipBlockComment(CodeSegment withinSegment)
		{
			Skip(2);	// Assumes this method is called while the position is just before '/*'
			var depth = 1;
			while (depth > 0 && !EndOfFile(withinSegment))
			{
				var ch = PeekChar(withinSegment);
				if (ch == '/' && Peek(2, withinSegment) == "/*")
				{
					Skip(2);
					depth++;
				}
				else if (ch == '*' && Peek(2, withinSegment) == "*/")
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

		public int MainPosition
		{
			get
			{
				if (_segments.Count == 0) return _mainLength;

				return _segments[0].Position;
			}
		}

		public DocPosition DocPosition
		{
			get
			{
				if (_segments.Count == 0) return new DocPosition(_doc, _mainLength);

				var seg = _segments[_segments.Count - 1];
				return new DocPosition(seg.Document, seg.Position + seg.StartingOffset);
			}
		}
		#endregion

		#region Preprocessing
		private IEnumerable<CodeToken> ReadPreprocessor(CodeToken directiveToken, CodeSegment directiveSegment)
		{
			switch (directiveToken.Text)
			{
				case "#define":
					foreach (var t in ReadDefine(directiveToken, directiveSegment)) yield return t;
					break;
				case "#elif":
					Log.Error("#elif not implemented");	// TODO
					yield return directiveToken;
					break;
				case "#else":
					Log.Error("#else not implemented"); // TODO
					yield return directiveToken;
					break;
				case "#endif":
					Log.Error("#endif not implemented");    // TODO
					yield return directiveToken;
					break;
				case "#if":
					Log.Error("#if not implemented");   // TODO
					yield return directiveToken;
					break;
				case "#ifdef":
					Log.Error("#ifdef not implemented");    // TODO
					yield return directiveToken;
					break;
				case "#ifndef":
					Log.Error("#ifndef not implemented");   // TODO
					yield return directiveToken;
					break;
				case "#include":
					foreach (var t in ReadInclude(directiveToken, directiveSegment)) yield return t;
					break;
				case "#insert":
					Log.Error("#insert not implemented");   // TODO
					yield return directiveToken;
					break;
				case "#label":
					Log.Error("#label not implemented");    // TODO
					yield return directiveToken;
					break;
				case "#replace":
					Log.Error("#replace not implemented");  // TODO
					yield return directiveToken;
					break;
				case "#undef":
					Log.Error("#undef not implemented");    // TODO
					yield return directiveToken;
					break;
				case "#warnadd":
					Log.Error("#warnadd not implemented");  // TODO
					yield return directiveToken;
					break;
				case "#warndel":
					Log.Error("#warndel not implemented");  // TODO
					yield return directiveToken;
					break;
				default:
					Log.Warning("Unrecognized preprocessor directive '{0}'.", directiveToken.Text);
					yield return directiveToken;
					break;
			}
		}

		private IEnumerable<CodeToken> ReadDefine(CodeToken directiveToken, CodeSegment directiveSegment)
		{
			if (!directiveToken.MainSpan.IsEmpty) yield return directiveToken;

			SkipWhite(directiveSegment);

			var nameMainPos = MainPosition;
			var nameDocPos = DocPosition;
			var name = ReadWord(directiveSegment);
			if (string.IsNullOrEmpty(name))
			{
				Log.Warning("No name after #define at {0}", nameDocPos);
				yield return directiveToken.ToInvalid();
				yield break;
			}

			// Read the macro name
			var nameToken = new CodeToken(
				text: name,
				type: CodeType.Word,
				mainSpan: new Span(nameMainPos, MainPosition - nameMainPos),
				position: nameDocPos,
				compiled: false);

			// Read optional arguments
			List<string> argNames = null;
			if (PeekChar(directiveSegment) == '(')
			{
				// This is a macro with parameters
				argNames = new List<string>();
				while (!EndOfFile(directiveSegment))
				{
					SkipWhite(directiveSegment);
					if (ReadExact(')', directiveSegment)) break;
					if (ReadExact(',', directiveSegment)) continue;

					var argName = ReadWord(directiveSegment);
					if (!string.IsNullOrEmpty(argName)) argNames.Add(argName);
					else break;
				}
			}

			var bodyTokens = new List<CodeToken>(); // Tokens to be displayed to the user
			var defTokens = new List<CodeToken>();  // Tokens that will be included in the macro
			SkipWhite(directiveSegment, stayOnSameLine: true);
			if (PeekExact('{', directiveSegment))
			{
				// The body is enclosed in { }

				var bodyStartMainPos = MainPosition;
				var bodyStartDocPos = DocPosition;
				Skip(1);
				bodyTokens.Add(new CodeToken(
					text: "{",
					type: CodeType.OpenBrace,
					mainSpan: new Span(bodyStartMainPos, MainPosition - bodyStartMainPos),
					position: bodyStartDocPos,
					compiled: false));

				while (!EndOfFile(directiveSegment))
				{
					SkipWhite(directiveSegment);
					if (PeekExact('}', directiveSegment))
					{
						var bodyEndMainPos = MainPosition;
						var bodyEndDocPos = DocPosition;
						Skip(1);
						bodyTokens.Add(new CodeToken(
							text: "}",
							type: CodeType.CloseBrace,
							mainSpan: new Span(bodyEndMainPos, MainPosition - bodyEndMainPos),
							position: bodyEndDocPos,
							compiled: false));
						break;
					}

					foreach (var tok in ReadSimpleNestable(directiveSegment, CodeType.CloseBrace))
					{
						if (argNames != null && tok.Type == CodeType.Word && argNames.Contains(tok.Text))
						{
							var argToken = new CodeToken(
								text: tok.Text,
								type: CodeType.PreprocessorArgument,
								mainSpan: tok.MainSpan,
								position: tok.Position,
								compiled: false);
							bodyTokens.Add(argToken);
							defTokens.Add(argToken);
						}
						else
						{
							bodyTokens.Add(tok.ToNotCompiled());
							defTokens.Add(tok);
						}
					}
				}
			}
			else
			{
				// Single line optionally extended with trailing \
				SkipWhite(directiveSegment, stayOnSameLine: true);
				while (!EndOfFile(directiveSegment))
				{
					var lineMainPos = MainPosition;
					var lineDocPos = DocPosition;
					var line = ReadRawTextToEndOfLine(directiveSegment);
					var more = false;
					if (line.EndsWith("\\"))
					{
						line = line.Substring(0, line.Length - 1);
						more = true;
					}

					var rdr = new SimpleCodeReader(line, lineMainPos, lineDocPos);
					foreach (var tok in rdr.ReadAll())
					{
						if (argNames != null && tok.Type == CodeType.Word && argNames.Contains(tok.Text))
						{
							var argToken = new CodeToken(
								text: tok.Text,
								type: CodeType.PreprocessorArgument,
								mainSpan: tok.MainSpan,
								position: tok.Position,
								compiled: false);
							bodyTokens.Add(argToken);
							defTokens.Add(argToken);
						}
						else
						{
							bodyTokens.Add(tok.ToNotCompiled());
							defTokens.Add(tok);
						}
					}

					if (!more) break;
				}
			}

			AddMacro(new Macro(
				name: name,
				arguments: argNames != null ? argNames.ToArray() : null,
				bodyTokens: defTokens.Count != 0 ? defTokens.ToArray() : null));

			yield return nameToken;
			foreach (var t in bodyTokens) yield return t;
		}

		private IEnumerable<CodeToken> ReadInclude(CodeToken directiveToken, CodeSegment directiveSegment)
		{
			yield return directiveToken;

			var fileNameSB = new StringBuilder();

			SkipWhite(directiveSegment, stayOnSameLine: true);
			var startCh = PeekChar(directiveSegment);
			if (startCh == '<' || startCh == '\"')
			{
				var endCh = startCh == '<' ? '>' : '\"';
				var includeSystemPaths = startCh == '<';
				var startMainPos = MainPosition;
				var startDocPos = DocPosition;
				Skip(1);

				while (!EndOfFile(directiveSegment))
				{
					var ch = ReadChar(directiveSegment);
					if (ch == endCh) break;
					fileNameSB.Append(ch);
				}

				var endMainPos = MainPosition;
				yield return new CodeToken(
					text: string.Concat(startCh, fileNameSB, endCh),
					type: CodeType.StringLiteral,
					mainSpan: new Span(startMainPos, endMainPos - startMainPos),
					position: startDocPos,
					compiled: false);

				var includeDoc = _profile.TryGetIncludeFile(fileNameSB.ToString(), directiveSegment.Document, includeSystemPaths);
				if (includeDoc != null)
				{
					_segments.Add(new CodeSegment(
						document: includeDoc,
						startingOffset: 0,
						code: includeDoc.Text,
						position: 0,
						returnOffset: CurrentSegment?.Position ?? 0));
				}
				else
				{
					Log.Warning("Include file {2}{0}{3} not found from document '{1}'", fileNameSB, directiveSegment.Document, startCh, endCh);
				}
			}
			else
			{
				yield return directiveToken;
			}
		}
		#endregion

		#region CodeSegment
		internal class CodeSegment
		{
			/// <summary>
			/// Document in which this code lives.
			/// </summary>
			public Document Document { get; set; }

			/// <summary>
			/// Starting position of the code segment to be read.
			/// </summary>
			public int StartingOffset { get; set; }

			/// <summary>
			/// The chunk of code to be read.
			/// </summary>
			public string Code { get; set; }

			/// <summary>
			/// The current position in the segment.
			/// </summary>
			public int Position { get; set; }

			/// <summary>
			/// Location in the parent excerpt to return to when reading this code is done.
			/// </summary>
			public int ReturnOffset { get; set; }

			public CodeSegment(Document document, int startingOffset, string code, int position, int returnOffset)
			{
				Document = document ?? throw new ArgumentNullException(nameof(document));
				Code = code ?? throw new ArgumentNullException(nameof(code));

				if (startingOffset < 0 || startingOffset >= code.Length) throw new ArgumentOutOfRangeException(nameof(startingOffset));
				StartingOffset = startingOffset;

				if (position < 0 || position >= code.Length) throw new ArgumentOutOfRangeException(nameof(position));
				Position = position;

				ReturnOffset = returnOffset;
			}

			public CodeSegment Clone()
			{
				return new CodeSegment(Document, StartingOffset, Code, Position, ReturnOffset);
			}
		}

		private CodeSegment CurrentSegment => _segments.Count == 0 ? null : _segments[_segments.Count - 1];
		#endregion

		#region Snapshots
		/// <summary>
		/// Takes a snapshot of the reader that can be restored if the previous position/state is needed.
		/// </summary>
		/// <returns>The snapshot object.</returns>
		public Snapshot TakeSnapshot()
		{
			return new Snapshot(this);
		}

		public class Snapshot
		{
			private PreprocessingCodeReader _reader;
			private List<CodeSegment> _segments;
			private bool _lastTokenSupportsNegative;

			internal Snapshot(PreprocessingCodeReader reader)
			{
				_segments = new List<CodeSegment>();
				_reader = reader ?? throw new ArgumentNullException(nameof(reader));
				foreach (var seg in _reader._segments)
				{
					_segments.Add(seg.Clone());
				}

				_lastTokenSupportsNegative = _reader._lastTokenSupportsNegative;
			}

			public void Restore()
			{
				_reader._segments.Clear();
				foreach (var seg in _segments)
				{
					_reader._segments.Add(seg.Clone());
				}

				_reader._lastTokenSupportsNegative = _lastTokenSupportsNegative;
			}
		}
		#endregion

		#region Macros
		private Dictionary<string, Macro> _macros = new Dictionary<string, Macro>();

		public void AddMacro(Macro macro)
		{
			_macros[macro.Name] = macro;
		}

		public class Macro
		{
			public string Name { get; private set; }
			public string[] Arguments { get; private set; }
			public CodeToken[] BodyTokens { get; private set; }

			/// <summary>
			/// Creates a macro definition
			/// </summary>
			/// <param name="name">Name of the macro</param>
			/// <param name="arguments">Optional arguments. Pass null if no arguments present.</param>
			/// <param name="bodyTokens">Optional body tokens. Pass null if no body tokens present.</param>
			public Macro(string name, string[] arguments, CodeToken[] bodyTokens)
			{
				if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
				Name = name;

				Arguments = arguments;
				if (Arguments != null && Arguments.Length == 0) Arguments = null;

				BodyTokens = bodyTokens;
				if (BodyTokens != null && BodyTokens.Length == 0) BodyTokens = null;
			}
		}
		#endregion
	}
}
