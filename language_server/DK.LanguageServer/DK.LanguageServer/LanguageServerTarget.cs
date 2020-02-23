using System;
using System.Collections.Generic;
using System.Text;
using DK.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace DK.LanguageServer
{
	class LanguageServerTarget
	{
		private LanguageServer _server;

		public LanguageServerTarget(LanguageServer server)
		{
			_server = server ?? throw new ArgumentNullException(nameof(server));
		}

		[JsonRpcMethod(Methods.InitializeName)]
		public object Initialize(JToken arg)
		{
			Log.Debug("Received initialize message");

			return new InitializeResult
			{
				Capabilities = new ServerCapabilities
				{
					TextDocumentSync = new TextDocumentSyncOptions
					{
						OpenClose = true,
						Change = TextDocumentSyncKind.Incremental
					}
				}
			};
		}

		[JsonRpcMethod(Methods.ShutdownName)]
		public object OnShutdown()
		{
			_server.OnShutdown();
			return null;
		}

		[JsonRpcMethod(Methods.ExitName)]
		public void OnExit()
		{
			_server.OnExit();
		}

		[JsonRpcMethod(Methods.TextDocumentDidOpenName)]
		public void OnTextDocumentOpened(JToken arg)
		{
			var e = arg.ToObject<DidOpenTextDocumentParams>();
			Log.Debug("Text document opened: {0} Version {1}", e.TextDocument.Uri.AbsoluteUri, e.TextDocument.Version);
			_server.OnTextDocumentOpened(e.TextDocument);
		}

		[JsonRpcMethod(Methods.TextDocumentDidCloseName)]
		public void OnTextDocumentClosed(JToken arg)
		{
			var e = arg.ToObject<DidCloseTextDocumentParams>();
			Log.Debug("Text document closed: {0}", e.TextDocument.Uri.AbsoluteUri);
			_server.OnTextDocumentClosed(e.TextDocument);
		}

		[JsonRpcMethod(Methods.TextDocumentDidChangeName)]
		public void OnTextDocumentChanged(JToken arg)
		{
			var e = arg.ToObject<DidChangeTextDocumentParams>();

			Log.Debug("Text document changed: {0} Version {1}", e.TextDocument.Uri.AbsoluteUri, e.TextDocument.Version);
			foreach (var change in e.ContentChanges)
			{
				Log.Debug("Range [Ln{0}Ch{1}-Ln{2}Ch{3}] RangeLength [{4}] Text[{5}]",
					change.Range.Start.Line, change.Range.Start.Character,
					change.Range.End.Line, change.Range.End.Character,
					change.RangeLength,
					change.Text);
			}

			_server.OnTextDocumentChanged(e.TextDocument, e.ContentChanges);
		}
	}
}
