using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Common
{
	class DocumentNotFoundException : Exception
	{
		public Uri Uri { get; set; }

		public DocumentNotFoundException(Uri uri)
			: base($"Document not found: uri")
		{
			Uri = uri;
		}
	}
}
