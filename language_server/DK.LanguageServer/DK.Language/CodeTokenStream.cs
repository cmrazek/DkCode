using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	public delegate IEnumerable<CodeToken> CodeTokenFilterManyDelegate(CodeToken token);
	public delegate CodeToken? CodeTokenFilterOneDelegate(CodeToken token);

	public abstract class CodeTokenStream
	{
		public abstract void Write(CodeToken token);

		public CodeTokenStreamFilterMany FilterMany(CodeTokenFilterManyDelegate callback)
		{
			return new CodeTokenStreamFilterMany(this, callback);
		}

		public CodeTokenStreamFilterOne FilterOne(CodeTokenFilterOneDelegate callback)
		{
			return new CodeTokenStreamFilterOne(this, callback);
		}

		public CodeTokenStreamDistributor Distribute(params CodeTokenStream[] streams)
		{
			return new CodeTokenStreamDistributor(this, streams);
		}
	}

	internal class EndOfCodeTokenStreamException : Exception
	{
		public EndOfCodeTokenStreamException() { }
	}

	public class CodeTokenStreamFilterMany : CodeTokenStream
	{
		private CodeTokenStream _stream;
		private CodeTokenFilterManyDelegate _callback;

		public CodeTokenStreamFilterMany(CodeTokenStream stream, CodeTokenFilterManyDelegate callback)
		{
			_stream = stream ?? throw new ArgumentNullException(nameof(stream));
			_callback = callback ?? throw new ArgumentNullException(nameof(callback));
		}

		public override void Write(CodeToken token)
		{
			var list = _callback(token);
			if (list == null) return;

			foreach (var t in list)
			{
				_stream.Write(t);
			}
		}
	}

	public class CodeTokenStreamFilterOne : CodeTokenStream
	{
		private CodeTokenStream _stream;
		private CodeTokenFilterOneDelegate _callback;

		public CodeTokenStreamFilterOne(CodeTokenStream stream, CodeTokenFilterOneDelegate callback)
		{
			_stream = stream ?? throw new ArgumentNullException(nameof(stream));
			_callback = callback ?? throw new ArgumentNullException(nameof(callback));
		}

		public override void Write(CodeToken token)
		{
			var t = _callback(token);
			if (t.HasValue) _stream.Write(t.Value);
		}
	}

	public class CodeTokenStreamDistributor : CodeTokenStream
	{
		private CodeTokenStream _parent;
		private CodeTokenStream[] _streams;

		public CodeTokenStreamDistributor(CodeTokenStream parent, params CodeTokenStream[] streams)
		{
			_parent = parent ?? throw new ArgumentNullException(nameof(parent));
			_streams = streams ?? throw new ArgumentNullException(nameof(streams));
		}

		public override void Write(CodeToken token)
		{
			_parent.Write(token);

			foreach (var stream in _streams)
			{
				stream.Write(token);
			}
		}
	}

	public delegate void CodeTokenListenerDelegate(CodeToken token);

	public class CodeTokenListener : CodeTokenStream
	{
		private CodeTokenListenerDelegate _callback;

		public CodeTokenListener(CodeTokenListenerDelegate callback)
		{
			_callback = callback ?? throw new ArgumentNullException(nameof(callback));
		}

		public override void Write(CodeToken token)
		{
			_callback(token);
		}
	}
}
