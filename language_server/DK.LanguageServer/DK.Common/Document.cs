using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Common
{
	public class Document
	{
		private Uri _uri;
		private string _text;
		private int _version;

		public Document(Uri uri, string text, int version)
		{
			_uri = uri;
			_text = text;
			_version = version;
		}

		public Uri Uri => _uri;

		public string Text
		{
			get => _text;
			set => _text = value;
		}

		public int Version
		{
			get => _version;
			set => _version = value;
		}
	}
}
