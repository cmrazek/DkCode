using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DK.Common;

namespace DK.Language.Preprocessor
{
	class ConditionResolver
	{
		public ConditionResolver()
		{
		}

		public bool? Resolve(string conditionText, MacroStore macros)
		{
			if (macros == null) throw new ArgumentNullException(nameof(macros));

			var rdr = new SimpleCodeReader(conditionText, 0, DocPosition.Empty);
			var resolvedTokens = ResolveTokens(new CodeTokenStream(rdr.ReadAll()), macros).ToArray();
			var group = new Node(GroupifyTokens(new CodeTokenStream(resolvedTokens)));

			var value = group.GetValue();
			if (value.HasValue) return value.Value != 0;
			return null;
		}

		private CodeTokenStream ResolveTokens(CodeTokenStream stream, MacroStore macros)
		{
			var outStream = new CodeTokenStream();

			while (!stream.EndOfStream)
			{
				var token = stream.Read();
				if (token.Type == CodeType.Word && macros.TryGetMacro(token.Text, out var macro))
				{
					if (macro.HasBody)
					{
						if (macro.HasArguments)
						{
							var savePos = stream.Position;
							var args = ParseArgumentTokens(stream, macros);
							var argNames = macro.Arguments;
							if (args.Length != argNames.Length)
							{
								Log.Warning("Call of macro '{0}' has {1} argument(s) but expected {2}.", macro.Name, args.Length, argNames.Length);
								// Just add the token name as-is
								stream.Position = savePos + 1;
								outStream.Add(token);
							}
							else
							{
								var subMacros = macros.AddScope();
								for (int a = 0; a < args.Length; a++) subMacros.Add(new Macro(argNames[a], null, args[a]));
								outStream.Add(ResolveTokens(macro.Body, subMacros));
							}
						}
						else
						{
							outStream.Add(ResolveTokens(macro.Body, macros));
						}
					}
					else
					{
						// Macro has no body so this can simply be omitted
					}
				}
				else
				{
					outStream.Add(token);
				}
			}

			return outStream;
		}

		private CodeTokenStream[] ParseArgumentTokens(CodeTokenStream stream, MacroStore macros)
		{
			var args = new List<CodeTokenStream>();

			if (stream.EndOfStream || stream.Peek().Type != CodeType.OpenBracket)
			{
				return new CodeTokenStream[0];
			}

			stream.Read();

			var curArg = new CodeTokenStream();
			var stack = new Stack<CodeType>();
			while (!stream.EndOfStream)
			{
				var token = stream.Read();
				if (stack.Count > 0 && stack.Peek() == token.Type)
				{
					curArg.Add(token);
					stack.Pop();
					continue;
				}
				if (token.Type == CodeType.CloseBracket)
				{
					break;
				}
				if (token.Type == CodeType.Comma)
				{
					args.Add(curArg);
					curArg = new CodeTokenStream();
					continue;
				}
				if (token.Type == CodeType.OpenArray || token.Type == CodeType.OpenBrace || token.Type == CodeType.OpenBracket)
				{
					curArg.Add(token);
					stack.Push(token.Type.GetClosingType());
					continue;
				}

				curArg.Add(token);
			}

			args.Add(curArg);
			var retArgs = new List<CodeTokenStream>();
			foreach (var arg in args)
			{
				retArgs.Add(ResolveTokens(arg, macros));
			}
			return retArgs.ToArray();
		}

		private IEnumerable<Node> GroupifyTokens(CodeTokenStream stream)
		{
			while (!stream.EndOfStream)
			{
				var token = stream.Read();
				if (token.Type == CodeType.OpenBracket)
				{
					yield return ReadTokenGroup(stream);
				}
				else
				{
					yield return new Node(token);
				}
			}
		}

		private Node ReadTokenGroup(CodeTokenStream stream)
		{
			var nodes = new List<Node>();

			while (!stream.EndOfStream)
			{
				var token = stream.Read();
				if (token.Type == CodeType.CloseBracket) break;
				if (token.Type == CodeType.OpenBracket)
				{
					nodes.Add(ReadTokenGroup(stream));
				}
				else
				{
					nodes.Add(new Node(token));
				}
			}

			return new Node(nodes);
		}

		private enum NodeType
		{
			Group,
			Token,
			Value
		}

		private class Node
		{
			private NodeType _type;
			private CodeToken? _token;
			private Node[] _nodes;
			private decimal? _value;

			public Node(IEnumerable<Node> groups)
			{
				_type = NodeType.Group;
				_nodes = groups.ToArray();
			}

			public Node(CodeToken token)
			{
				_type = NodeType.Token;
				_token = token;
			}

			public Node(decimal? value)
			{
				_type = NodeType.Value;
				_value = value;
			}

			public bool HasValue => _type == NodeType.Value || _type == NodeType.Token;

			public decimal? GetValue()
			{
				if (_type == NodeType.Value) return _value;

				if (_type == NodeType.Token)
				{
					if (_token.Value.Type == CodeType.Number)
					{
						if (decimal.TryParse(_token.Value.Text, out var result))
						{
							_type = NodeType.Value;
							_value = result;
							return result;
						}
						else
						{
							_type = NodeType.Value;
							_value = null;
							return null;
						}
					}
					else
					{
						_type = NodeType.Value;
						_value = null;
						return null;
					}
				}

				var nodes = _nodes.ToList();

				while (true)
				{
					var highestPrec = 0;
					foreach (var node in nodes)
					{
						var prec = node._type == NodeType.Token ? node._token.Value.Type.GetPrecedence() : 0;
						if (prec > highestPrec) highestPrec = prec;
					}

					if (highestPrec == 0) break;

					if (highestPrec % 2 == 0)
					{
						// left-to-right
						for (int n = 0; n < nodes.Count; n++)
						{
							var node = nodes[n];
							if (node._type == NodeType.Token && node._token.Value.Type.GetPrecedence() == highestPrec)
							{
								node.Simplify(nodes, n);
								break;
							}
						}
					}
					else
					{
						// right-to-left
						for (int n = nodes.Count - 1; n >= 0; n--)
						{
							var node = nodes[n];
							if (node._type == NodeType.Token && node._token.Value.Type.GetPrecedence() == highestPrec)
							{
								node.Simplify(nodes, n);
								break;
							}
						}
					}
				}

				if (nodes.Count != 1 || !nodes[0].HasValue) _value = null;
				else _value = nodes[0].GetValue();

				_type = NodeType.Value;
				_nodes = null;
				return _value;
			}

			public void Simplify(List<Node> nodes, int index)
			{
				if (_nodes != null) throw new InvalidOperationException("This token is not eligible for simplification");

				switch (_token.Value.Type)
				{
					case CodeType.Add:
					case CodeType.Subtract:
					case CodeType.Multiply:
					case CodeType.Divide:
					case CodeType.Modulus:
					case CodeType.Less:
					case CodeType.LessEqual:
					case CodeType.Greater:
					case CodeType.GreaterEqual:
					case CodeType.Equal:
					case CodeType.NotEqual:
					case CodeType.And:
					case CodeType.Or:
						{
							var lNode = GetLeftNode(nodes, index);
							var rNode = GetRightNode(nodes, index);
							var lValue = lNode?.GetValue();
							var rValue = rNode?.GetValue();
							decimal? value = null;
							if (lValue.HasValue && rValue.HasValue)
							{
								switch (_token.Value.Type)
								{
									case CodeType.Add:
										value = lValue.Value + rValue.Value;
										break;
									case CodeType.Subtract:
										value = lValue.Value - rValue.Value;
										break;
									case CodeType.Multiply:
										value = lValue.Value * rValue.Value;
										break;
									case CodeType.Divide:
										if (rValue.Value != 0) value = lValue.Value / rValue.Value;
										break;
									case CodeType.Modulus:
										if (rValue.Value != 0) value = lValue.Value % rValue.Value;
										break;
									case CodeType.Less:
										value = lValue.Value < rValue.Value ? 1 : 0;
										break;
									case CodeType.LessEqual:
										value = lValue.Value <= rValue.Value ? 1 : 0;
										break;
									case CodeType.Greater:
										value = lValue.Value > rValue.Value ? 1 : 0;
										break;
									case CodeType.GreaterEqual:
										value = lValue.Value >= rValue.Value ? 1 : 0;
										break;
									case CodeType.Equal:
										value = lValue.Value == rValue.Value ? 1 : 0;
										break;
									case CodeType.NotEqual:
										value = lValue.Value != rValue.Value ? 1 : 0;
										break;
									case CodeType.And:	// TODO: short circuit rValue
										value = lValue.Value != 0 && rValue.Value != 0 ? 1 : 0;
										break;
									case CodeType.Or:   // TODO: short circuit rValue
										value = lValue.Value != 0 || rValue.Value != 0 ? 1 : 0;
										break;
								}
							}

							nodes.Insert(index, new Node(value));
							if (lNode != null) nodes.Remove(lNode);
							if (rNode != null) nodes.Remove(rNode);
							nodes.Remove(this);
						}
						break;

					case CodeType.Ternary:
						{
							var lNode = GetLeftNode(nodes, index);
							var tNode = GetRightNode(nodes, index);
							var cNode = index + 2 < nodes.Count ? nodes[index + 2] : null;
							var fNode = index + 3 < nodes.Count ? nodes[index + 3] : null;

							decimal? value = null;
							if (cNode != null && cNode._type == NodeType.Token && cNode._token.Value.Type == CodeType.Colon)
							{
								var lValue = lNode?.GetValue();
								if (lValue.HasValue)
								{
									value = lValue.Value != 0 ? tNode?.GetValue() : fNode?.GetValue();
								}

								if (tNode != null) nodes.Remove(tNode);
								nodes.Remove(cNode);
								if (fNode != null) nodes.Remove(fNode);
							}

							nodes.Insert(index, new Node(value));
							if (lNode != null) nodes.Remove(lNode);
						}
						break;

					default:
						// Not applicable to a preprocessor condition statement
						nodes.Remove(this);
						break;
				}
			}

			private Node GetLeftNode(List<Node> groups, int index)
			{
				if (index <= 0) return null;
				return groups[index - 1];
			}

			private Node GetRightNode(List<Node> groups, int index)
			{
				if (index + 1 >= groups.Count) return null;
				return groups[index + 1];
			}
		}
	}
}
