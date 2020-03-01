using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DK.Common;
using DK.Language.Preprocessor;

namespace DK.Language
{
	/// <summary>
	/// Reads code while applying preprocessing inline as it's encountered
	/// </summary>
	public class PreprocessingCodeReader : BaseCodeReader
	{
		private DkProfile _profile;
		private Uri _uri;
		private Document _doc;
		private DocStringBuilder _code = new DocStringBuilder();
		private int _pos;
		private MacroStore _macros = new MacroStore();

		public PreprocessingCodeReader(DkProfile profile, Uri uri, Document document)
		{
			_profile = profile ?? throw new ArgumentNullException(nameof(profile));
			_uri = uri ?? throw new ArgumentNullException(nameof(uri));
			_doc = document != null ? document : profile.GetDocument(uri);
			_code.Append(_doc.Text, 0, new DocPosition(_doc, 0), DocCharFlags.Visible);
		}

		#region Reading Methods
		public override void ReadAll(CodeTokenStream stream)
		{
			SkipWhite();

			while (!EndOfFile)
			{
				var token = ReadSimple();
				if (!token.HasValue) continue;

				if (token.Value.Type == CodeType.Preprocessor)
				{
					ReadPreprocessor(stream, token.Value);
				}
				else if (token.Value.Type == CodeType.Word)
				{
					if (_macros.TryGetMacro(token.Value.Text, out var macro))
					{
						ReadMacroUsage(stream, token.Value.ToNotCompiled(), macro);
					}
					else
					{
						stream.Write(token.Value);
					}
				}
				else
				{
					stream.Write(token.Value);
				}
			}
		}

		public override int Position
		{
			get => _pos;
			set
			{
				if (value < 0 || value > _build.Length) throw new ArgumentOutOfRangeException();
				_pos = value;
			}
		}

		public override char PeekChar()
		{
			if (_pos >= _code.Length) return '\0';
			return _code[_pos].Char;
		}

		public int MainPosition => _code.GetMainPosition(_pos);

		public override void Skip(int count)
		{
			if (_pos + count <= _code.Length) _pos += count;
			else _pos = _code.Length;
		}

		public override bool SkipExact(char ch)
		{
			if (_pos < _code.Length && _code[_pos].Char == ch)
			{
				_pos++;
				return true;
			}
			return false;
		}

		public override char PeekChar(int offset)
		{
			if (_pos >= _code.Length) return '\0';
			return _code[_pos].Char;
		}

		public override string PeekString(int length)
		{
			if (_pos + length <= _code.Length)
			{
				return _code.Substring(_pos, length).Text;
			}
			else
			{
				return _code.Substring(_pos).Text;
			}
		}
		#endregion

		#region Building
		private DocStringBuilder _build = new DocStringBuilder();

		protected override void BuildClear()
		{
			_build.Clear();
		}

		protected override void Build(int count)
		{
			if (_pos + count <= _code.Length)
			{
				_build.Append(_code.Substring(_pos, count));
				_pos += count;
			}
			else
			{
				_build.Append(_code.Substring(_pos, _code.Length - _pos));
				_pos = _code.Length;
			}
		}

		protected override string BuildToString()
		{
			return _build.ToString();
		}

		protected override CodeToken BuildToToken(CodeType type, bool compiled)
		{
			return new CodeToken(_build.ToDocString(), type, compiled);
		}

		protected override bool BuildExact(char ch)
		{
			if (_pos < _code.Length && _code[_pos].Char == ch)
			{
				_build.Clear();
				_build.Append(_code.Substring(_pos, 1));
				return true;
			}
			return false;
		}

		protected override string BuildWord()
		{
			_build.Clear();
			var first = true;
			while (_pos < _code.Length && IsWordChar(_code[_pos].Char, first))
			{
				_build.Append(_code[_pos++]);
				first = false;
			}
			return _build.ToString();
		}

		public override bool EndOfFile => _pos >= _code.Length;
		#endregion

		#region Preprocessing
		private void ReadPreprocessor(CodeTokenStream stream, CodeToken directiveToken)
		{
			switch (directiveToken.Text)
			{
				case "#define":
					ReadDefine(stream, directiveToken);
					return;
				case "#elif":
					Log.Error("#elif not implemented"); // TODO
					stream.Write(directiveToken);
					return;
				case "#else":
					Log.Error("#else not implemented"); // TODO
					stream.Write(directiveToken);
					return;
				case "#endif":
					Log.Error("#endif not implemented");    // TODO
					stream.Write(directiveToken);
					return;
				case "#if":
					Log.Error("#if not implemented");   // TODO
					stream.Write(directiveToken);
					return;
				case "#ifdef":
					Log.Error("#ifdef not implemented");    // TODO
					stream.Write(directiveToken);
					return;
				case "#ifndef":
					Log.Error("#ifndef not implemented");   // TODO
					stream.Write(directiveToken);
					return;
				case "#include":
					ReadInclude(stream, directiveToken);
					return;
				case "#insert":
					Log.Error("#insert not implemented");   // TODO
					stream.Write(directiveToken);
					return;
				case "#label":
					Log.Error("#label not implemented");    // TODO
					stream.Write(directiveToken);
					return;
				case "#replace":
					Log.Error("#replace not implemented");  // TODO
					stream.Write(directiveToken);
					return;
				case "#undef":
					Log.Error("#undef not implemented");    // TODO
					stream.Write(directiveToken);
					return;
				case "#warnadd":
					Log.Error("#warnadd not implemented");  // TODO
					stream.Write(directiveToken);
					return;
				case "#warndel":
					Log.Error("#warndel not implemented");  // TODO
					stream.Write(directiveToken);
					return;
				default:
					Log.Warning("Unrecognized preprocessor directive '{0}'.", directiveToken.Text);
					stream.Write(directiveToken);
					return;
			}
		}

		private void ReadDefine(CodeTokenStream stream, CodeToken directiveToken)
		{
			stream.Write(directiveToken);

			// Read the macro name
			SkipWhite(stayOnSameLine: true);
			var name = BuildWord();
			if (string.IsNullOrEmpty(name))
			{
				Log.Warning("No name after #define at {0}", _build.StartPosition);
				return;
			}
			stream.Write(BuildToToken(CodeType.MacroName, compiled: false));

			// Read optional arguments
			List<string> argNames = null;
			SkipWhite(stayOnSameLine: true);
			if (PeekChar() == '(')
			{
				Build(1);
				stream.Write(BuildToToken(CodeType.OpenBracket, compiled: false));

				// This is a macro with parameters
				argNames = new List<string>();
				while (!EndOfFile)
				{
					SkipWhite(stayOnSameLine: true);
					if (BuildExact(')'))
					{
						stream.Write(BuildToToken(CodeType.CloseBracket, compiled: false));
						break;
					}
					if (BuildExact(','))
					{
						stream.Write(BuildToToken(CodeType.Comma, compiled: false));
						continue;
					}

					var argName = BuildWord();
					if (!string.IsNullOrEmpty(argName))
					{
						argNames.Add(argName);
						stream.Write(BuildToToken(CodeType.Argument, compiled: false));
					}
					else break;
				}
			}

			// Read macro body
			var bodyTokens = new CodeTokenCollection();
			SkipWhite(stayOnSameLine: true);
			if (BuildExact('{'))
			{
				// The body is enclosed in { }
				stream.Write(BuildToToken(CodeType.OpenBrace, compiled: false));

				SkipWhite();
				while (!EndOfFile)
				{
					if (BuildExact('}'))
					{
						stream.Write(BuildToToken(CodeType.CloseBrace, compiled: false));
						break;
					}

					ReadSimpleNestable(stream.FilterOne(token =>
					{
						if (argNames != null && token.Type == CodeType.Word && argNames.Contains(token.Text))
						{
							var argToken = token.ToType(CodeType.Argument);
							bodyTokens.Add(argToken);
							return argToken;
						}

						bodyTokens.Add(token);
						return token;
					}), CodeType.CloseBrace);

					SkipWhite();
				}
			}
			else
			{
				SkipWhite(stayOnSameLine: true);
				while (!EndOfFile)
				{
					var eol = SkipExact('\r');
					var eol2 = SkipExact('\n');
					if (eol || eol2) break;

					if (BuildExact('\\'))
					{
						if (IsEndOfLineChar(PeekChar()))
						{
							stream.Write(BuildToToken(CodeType.LineContinue, compiled: false));
							SkipExact('\r');
							SkipExact('\n');
						}
						else
						{
							stream.Write(BuildToToken(CodeType.Invalid, compiled: true));
						}
						continue;
					}

					var token = ReadSimple(stayOnSameLine: true);
					if (token.HasValue)
					{
						if (argNames != null && token.Value.Type == CodeType.Word && argNames.Contains(token.Value.Text))
						{
							var argToken = token.Value.ToType(CodeType.Argument);
							bodyTokens.Add(argToken);
							stream.Write(argToken);
						}
						else
						{
							bodyTokens.Add(token.Value);
							stream.Write(token.Value);
						}
					}

					SkipWhite(stayOnSameLine: true);
				}
			}

			_macros.Add(new Macro(name, argNames != null ? argNames.ToArray() : null, bodyTokens));
		}

		private void ReadMacroUsage(CodeTokenStream stream, CodeToken nameToken, Macro macro)
		{
			stream.Write(nameToken.ToNotCompiled().ToType(CodeType.MacroName));
			if (!macro.HasBody) return;

			if (macro.HasArguments)
			{
				SkipWhite();
				if (PeekChar() != '(')
				{
					Log.Warning("Use of macro '{0}' requires arguments but none found.", nameToken.Text);
				}
				else
				{
					Build(1);
					stream.Write(BuildToToken(CodeType.OpenBracket, compiled: false));

					var args = new List<CodeTokenCollection>();
					var curArg = new CodeTokenCollection();
					SkipWhite();
					while (!EndOfFile)
					{
						if (PeekChar() == ')')
						{
							Build(1);
							stream.Write(BuildToToken(CodeType.CloseBracket, compiled: false));
							break;
						}
						if (PeekChar() == ',')
						{
							Build(1);
							stream.Write(BuildToToken(CodeType.Comma, compiled: false));
							SkipWhite();
							continue;
						}

						ReadSimpleNestable(stream.FilterOne(token =>
						{
							curArg.Add(token);
							return token.ToNotCompiled();
						}), CodeType.CloseBracket);

						SkipWhite();
					}

					var argNames = macro.Arguments;
					if (args.Count != argNames.Length)
					{
						Log.Warning("Macro '{0}' requires {1} arguments, but counted {2}.", macro.Name, argNames.Length, args.Count);
					}
					else
					{
						var subMacros = _macros.AddScope();
						for (int a = 0, aa = args.Count; a < aa; a++)
						{
							subMacros.Add(new Macro(argNames[a], null, args[a]));
						}

						ResolveTokens(stream.FilterOne(t => t.ToMainSpan(Span.FromPosition(MainPosition))), macro.Body, subMacros);
					}
				}
			}
			else // No arguments
			{
				ResolveTokens(stream.FilterOne(t => t.ToMainSpan(Span.FromPosition(MainPosition))), macro.Body, _macros);
			}
		}

		private void ResolveTokens(CodeTokenStream stream, CodeTokenCollection inStream, MacroStore macros)
		{
			var outStream = new CodeTokenCollection();

			while (true)
			{
				var gotMacro = false;

				while (!inStream.EndOfStream)
				{
					var token = inStream.Read();
					if (token.Type == CodeType.Word && macros.TryGetMacro(token.Text, out var macro))
					{
						gotMacro = true;
						if (macro.HasBody)
						{
							if (macro.HasArguments)
							{
								if (inStream.EndOfStream || inStream.Peek().Type != CodeType.OpenBracket)
								{
									outStream.Add(token);
								}
								else
								{
									var savePos = inStream.Position;
									var openArgsToken = inStream.Read();
									var args = new List<CodeTokenCollection>();
									var curArg = new CodeTokenCollection();
									var stack = new Stack<CodeType>();
									while (!inStream.EndOfStream)
									{
										var tok = inStream.Read();
										if (stack.Count > 0 && stack.Peek() == tok.Type)
										{
											stack.Pop();
										}
										else if (tok.Type == CodeType.CloseBracket)
										{
											break;
										}
										else if (tok.Type == CodeType.Comma)
										{
											args.Add(curArg);
											curArg = new CodeTokenCollection();
										}
										else if (tok.Type == CodeType.OpenArray || tok.Type == CodeType.OpenBrace || tok.Type == CodeType.OpenBracket)
										{
											stack.Push(tok.Type.GetClosingType());
										}
										else
										{
											curArg.Add(tok);
										}
									}
									args.Add(curArg);

									var argNames = macro.Arguments;
									if (args.Count != argNames.Length)
									{
										inStream.Position = savePos;
									}
									else
									{
										var subMacros = macros.AddScope();
										for (int a = 0, aa = args.Count; a < aa; a++) subMacros.Add(new Macro(argNames[a], null, args[a]));
										ResolveTokens(outStream, macro.Body, subMacros);
									}
								}
							}
							else
							{
								ResolveTokens(outStream, macro.Body, macros);
							}
						}
					}
					else
					{
						outStream.Add(token);
					}
				}

				if (gotMacro)
				{
					// Run it through the resolver another time until no more macros are found
					inStream = outStream;
					inStream.Position = 0;
					outStream = new CodeTokenCollection();
				}
				else break;
			}

			foreach (var t in outStream) stream.Write(t);
		}

		private void ReadInclude(CodeTokenStream stream, CodeToken directiveToken)
		{
			stream.Write(directiveToken);

			var fileNameSB = new StringBuilder();
			BuildClear();

			SkipWhite(stayOnSameLine: true);
			var startCh = PeekChar();
			if (startCh == '<' || startCh == '\"')
			{
				var endCh = startCh == '<' ? '>' : '\"';
				var includeSystemPaths = startCh == '<';
				Build(1);

				while (!EndOfFile)
				{
					var ch = PeekChar();
					if (ch == endCh)
					{
						Build(1);
						break;
					}
					fileNameSB.Append(ch);
				}

				stream.Write(BuildToToken(CodeType.StringLiteral, compiled: false));

				var includeDoc = _profile.TryGetIncludeFile(fileNameSB.ToString(), directiveToken.Position.Document, includeSystemPaths);
				if (includeDoc != null)
				{
					_code.InsertDoc(_pos, includeDoc.Text, includeDoc, DocCharFlags.None);
				}
				else
				{
					Log.Warning("Include file {2}{0}{3} not found from document '{1}'", fileNameSB, directiveToken.Position.Document, startCh, endCh);
				}
			}
		}
		#endregion
	}
}
