using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace DK.LanguageServer
{
	class Document
	{
		private Uri _uri;
		private string _text;
		private int _version;
		private Jobs.ValidateDocumentJob _validateJob;

		public Document(Uri uri, string text, int version)
		{
			_uri = uri;
			_text = text;
			_version = version;
		}

		public string Text => _text;
		public Uri Uri => _uri;
		public int Version => _version;

		public LSP.Position OffsetToLspPosition(int offset, int resumeOffset = 0, LSP.Position resumePosition = null)
		{
			if (offset <= 0) return new LSP.Position(0, 0);

			int startingOffset = 0;
			int lineOffset = 0;
			int chOffset = 0;

			if (resumeOffset > offset)
			{
				lineOffset = resumePosition.Line;
				chOffset = resumePosition.Character;
				startingOffset = resumeOffset;
			}

			for (int i = startingOffset, ii = _text.Length; i < offset && i < ii; i++)
			{
				var ch = _text[i];
				if (ch == '\n')
				{
					lineOffset++;
					chOffset = 0;
				}
				else if (ch != '\r')
				{
					chOffset++;
				}
			}

			return new LSP.Position(lineOffset, chOffset);
		}

		public int LspPositionToOffset(LSP.Position position, int resumeOffset = 0, LSP.Position resumePosition = null)
		{
			int startingOffset = 0;
			int lineOffset = 0;
			int chOffset = 0;

			if (resumePosition != null)
			{
				if (resumePosition.Line == position.Line && resumePosition.Character == position.Character)
				{
					return resumeOffset;
				}

				if (resumePosition.Line > position.Line ||
					(resumePosition.Line == position.Line && resumePosition.Character > position.Character))
				{
					lineOffset = resumePosition.Line;
					chOffset = resumePosition.Character;
					startingOffset = resumeOffset;
				}
			}

			for (int i = startingOffset, ii = _text.Length; i < ii; i++)
			{
				if (lineOffset == position.Line && chOffset == position.Character) return i;

				var ch = _text[i];
				if (ch == '\n')
				{
					if (lineOffset == position.Line) return i;
					lineOffset++;
					chOffset = 0;
				}
				else if (ch == '\r')
				{
					if (lineOffset == position.Line) return i;
				}
				else
				{
					chOffset++;
				}
			}

			return _text.Length;
		}

		public Span LspRangeToSpan(LSP.Range range)
		{
			var start = LspPositionToOffset(range.Start);
			var end = LspPositionToOffset(range.End, start, range.Start);
			if (end >= start) return new Span(start, end - start);
			else return new Span(start, 0);
		}

		public void ApplyChanges(IEnumerable<LSP.TextDocumentContentChangeEvent> changes, int? version)
		{
			if (!changes.Any()) return;

			var delta = 0;
			foreach (var change in changes) delta += string.IsNullOrEmpty(change.Text) ? 0 : change.Text.Length;
			var sb = new StringBuilder(_text.Length + delta);
			sb.Append(_text);

			foreach (var change in changes)
			{
				var span = LspRangeToSpan(change.Range);

				if (span.Length > 0)
				{
					if (span.Start < sb.Length)
					{
						if (span.End <= sb.Length)
						{
							sb.Remove(span.Start, span.Length);
						}
						else
						{
							sb.Remove(span.Start, sb.Length - span.Start);
						}
					}
				}

				if (!string.IsNullOrEmpty(change.Text))
				{
					if (span.Start < sb.Length)
					{
						sb.Insert(span.Start, change.Text);
					}
					else
					{
						sb.Append(change.Text);
					}
				}
			}

			_text = sb.ToString();
			if (version.HasValue) _version = version.Value;
		}

		public void Validate(LanguageServer server)
		{
			var rx = new System.Text.RegularExpressions.Regex(@"\b([A-Z]{3,})\b");
			var diags = new List<LSP.Diagnostic>();

			foreach (var err in rx.Matches(_text).Cast<System.Text.RegularExpressions.Match>())
			{
				var start = OffsetToLspPosition(err.Index);
				var end = OffsetToLspPosition(err.Index + err.Length, err.Index, start);

				diags.Add(new LSP.Diagnostic
				{
					Message = "Upper-case is bad, mmm-kay",
					Severity = LSP.DiagnosticSeverity.Warning,
					Range = new LSP.Range
					{
						Start = start,
						End = end
					}
				});
			}

			var parms = new LSP.PublishDiagnosticParams
			{
				Uri = _uri,
				Diagnostics = diags.ToArray()
			};

			server.JoinableTaskFactory.RunAsync(async () =>
			{
				await server.JoinableTaskFactory.SwitchToMainThreadAsync();
				await server.RpcNotifyAsync(LSP.Methods.TextDocumentPublishDiagnosticsName, parms);
			});
		}

		public Jobs.ValidateDocumentJob ValidateJob
		{
			get { return _validateJob; }
			set { _validateJob = value; }
		}
	}
}
