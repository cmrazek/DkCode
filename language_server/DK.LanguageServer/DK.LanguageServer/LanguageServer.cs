using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace DK.LanguageServer
{
	public class LanguageServer
	{
		private LanguageServerTarget _target;
		private JsonRpc _rpc;
		private ManualResetEvent _exitEvent = new ManualResetEvent(false);
		private Dictionary<string, TextDocumentItem> _docs = new Dictionary<string, TextDocumentItem>();

		public event EventHandler Disconnected;

		public LanguageServer(Stream sender, Stream reader)
		{
			_target = new LanguageServerTarget(this);
			_rpc = JsonRpc.Attach(sender, reader, _target);
			_rpc.Disconnected += OnRpcDisconnected;
		}

		private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
		{
			Disconnected?.Invoke(this, EventArgs.Empty);
		}

		public void OnShutdown()
		{
		}

		public void OnExit()
		{
			Disconnected?.Invoke(this, EventArgs.Empty);
		}

		public void OnTextDocumentOpened(TextDocumentItem doc)
		{
			_docs[doc.Uri.AbsoluteUri.ToLower()] = doc;
			ValidateFile(doc);
		}

		public void OnTextDocumentClosed(TextDocumentIdentifier docId)
		{
			_docs.Remove(docId.Uri.AbsoluteUri.ToLower());
		}

		public void OnTextDocumentChanged(TextDocumentIdentifier docId, TextDocumentContentChangeEvent[] changes)
		{
			//if (_docs.TryGetValue(docId.Uri.AbsoluteUri.ToLower(), out var doc))
			//{
			//	//ValidateFile(doc);
			//}
		}

		private void ValidateFile(TextDocumentItem doc)
		{
			Log.Debug("ValidateFile: {0}", doc.Uri.AbsoluteUri);	// TODO

			var rx = new Regex(@"\b([A-Z]{3,})\b");
			var diags = new List<Diagnostic>();

			foreach (var err in rx.Matches(doc.Text).Cast<Match>())
			{
				diags.Add(new Diagnostic
				{
					Message = "Upper-case is bad, mmm-kay",
					Severity = DiagnosticSeverity.Warning,
					Range = new LSP.Range
					{
						Start = OffsetToPosition(doc.Text, err.Index),
						End = OffsetToPosition(doc.Text, err.Index + err.Length)
					}
				});
			}

			_ = _rpc.NotifyWithParameterObjectAsync(LSP.Methods.TextDocumentPublishDiagnosticsName, new PublishDiagnosticParams
			{
				Uri = doc.Uri,
				Diagnostics = diags.ToArray()
			});
		}

		private Position OffsetToPosition(string content, int offset)
		{
			int line = 0;
			int pos = 0;

			for (int i = 0, ii = content.Length; i < offset && i < ii; i++)
			{
				var ch = content[i];
				if (ch == '\n')
				{
					line++;
					pos = 0;
				}
				else if (ch != '\r')
				{
					pos++;
				}
			}

			return new Position(line, pos);
		}
	}
}
