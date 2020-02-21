using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace DK.LanguageServer
{
	class LanguageServer
	{
		private LanguageServerTarget _target;
		private JsonRpc _rpc;
		private ManualResetEvent _exitEvent = new ManualResetEvent(false);
		private Dictionary<Uri, Document> _docs = new Dictionary<Uri, Document>();
		private WorkThread _workThread;
		private JoinableTaskContext _jtContext;
		private JoinableTaskFactory _jtFactory;

		public event EventHandler Disconnected;

		public LanguageServer(Stream sender, Stream reader)
		{
			_workThread = new WorkThread(this);
			_workThread.Start();

			_jtContext = new JoinableTaskContext(Thread.CurrentThread);
			_jtFactory = _jtContext.Factory;

			_target = new LanguageServerTarget(this);
			_rpc = JsonRpc.Attach(sender, reader, _target);
			_rpc.Disconnected += OnRpcDisconnected;
		}

		public JoinableTaskFactory JoinableTaskFactory => _jtFactory;

		private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
		{
			Disconnected?.Invoke(this, EventArgs.Empty);
		}

		public void OnShutdown()
		{
			_workThread.Kill();
		}

		public void OnExit()
		{
			Disconnected?.Invoke(this, EventArgs.Empty);
		}

		public void OnTextDocumentOpened(LSP.TextDocumentItem lspDoc)
		{
			var doc = new Document(lspDoc.Uri, lspDoc.Text, lspDoc.Version);
			_docs[doc.Uri] = doc;

			var job = new Jobs.ValidateDocumentJob(doc, this);
			doc.ValidateJob = job;
			_workThread.Enqueue(job);
		}

		public void OnTextDocumentClosed(LSP.TextDocumentIdentifier docId)
		{
			_docs.Remove(docId.Uri);
		}

		public void OnTextDocumentChanged(LSP.VersionedTextDocumentIdentifier docId, LSP.TextDocumentContentChangeEvent[] changes)
		{
			if (_docs.TryGetValue(docId.Uri, out var doc))
			{
				_workThread.Enqueue(new Jobs.ChangeDocumentJob(doc, changes, docId.Version));

				// TODO: This may need to be deferred
				var job = new Jobs.ValidateDocumentJob(doc, this);
				doc.ValidateJob = job;
				_workThread.Enqueue(job);
			}
		}

		public async Task RpcNotifyAsync(string targetName, object parameterObject)
		{
			await _rpc.NotifyWithParameterObjectAsync(targetName, parameterObject);
		}
	}
}
