using System;
using System.Collections.Generic;
using System.Text;

namespace DK.LanguageServer.Jobs
{
	class ValidateDocumentJob : BaseJob
	{
		private Document _doc;
		private LanguageServer _server;

		public ValidateDocumentJob(Document document, LanguageServer server)
		{
			_doc = document ?? throw new ArgumentNullException(nameof(document));
			_server = server ?? throw new ArgumentNullException(nameof(server));
		}

		public override void Execute()
		{
			if (_doc.ValidateJob == this)	// Don't validate if another validation has already been enqueued
			{
				_doc.Validate(_server);
			}
		}
	}
}
