using System;
using System.Collections.Generic;
using System.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace DK.LanguageServer.Jobs
{
	class ChangeDocumentJob : BaseJob
	{
		private Document _doc;
		private IEnumerable<LSP.TextDocumentContentChangeEvent> _changes;
		private int? _version;

		public ChangeDocumentJob(Document doc, IEnumerable<LSP.TextDocumentContentChangeEvent> changes, int? version)
		{
			_doc = doc ?? throw new ArgumentNullException(nameof(doc));
			_changes = changes ?? throw new ArgumentNullException(nameof(changes));
			_version = version;
		}

		public override void Execute()
		{
			_doc.ApplyChanges(_changes, _version);
		}
	}
}
