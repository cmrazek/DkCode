using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	public struct CodeToken
	{
		public string Text { get; private set; }
		public CodeType Type { get; private set; }
		public Span MainSpan { get; private set; }
		public DocPosition Position { get; private set; }
		public bool Compiled { get; private set; }

		public static readonly CodeToken None = new CodeToken(string.Empty, CodeType.None, Span.Empty, DocPosition.Empty, false);

		public CodeToken(string text, CodeType type, Span mainSpan, DocPosition position, bool compiled)
		{
			Text = text;
			Type = type;
			MainSpan = mainSpan;
			Position = position;
			Compiled = compiled;
		}

		internal CodeToken(DocString text, CodeType type, bool compiled)
		{
			Text = text.Text;
			MainSpan = text.MainSpan;
			Position = text.DocPosition;
			Type = type;
			Compiled = compiled;
		}

		public CodeToken ToInvalid()
		{
			return new CodeToken(Text, CodeType.Invalid, MainSpan, Position, Compiled);
		}

		public CodeToken ToNotCompiled()
		{
			return new CodeToken(Text, Type, MainSpan, Position, compiled: false);
		}

		public CodeToken ToType(CodeType type)
		{
			return new CodeToken(Text, type, MainSpan, Position, Compiled);
		}

		public CodeToken ToMainSpan(Span span)
		{
			return new CodeToken(Text, Type, span, Position, Compiled);
		}

		public override string ToString()
		{
			return $"{Type} '{Text}' {MainSpan} @{Position} Compiled:{Compiled}";
		}
	}
}
