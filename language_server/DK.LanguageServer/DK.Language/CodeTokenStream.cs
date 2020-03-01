using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DK.Language
{
	public class CodeTokenStream : IEnumerable<CodeToken>
	{
		private List<CodeToken> _tokens = new List<CodeToken>();
		private int _pos;

		public CodeTokenStream(IEnumerable<CodeToken> tokens)
		{
			_tokens.AddRange(tokens);
		}

		public CodeTokenStream(CodeToken token)
		{
			_tokens.Add(token);
		}

		public CodeTokenStream()
		{
		}

		public bool EndOfStream => _pos >= _tokens.Count;
		public bool IsEmpty => _tokens.Count == 0;

		public CodeToken Read()
		{
			if (_pos >= _tokens.Count) throw new EndOfCodeTokenStreamException();
			return _tokens[_pos++];
		}

		public CodeToken Peek()
		{
			if (_pos >= _tokens.Count) throw new EndOfCodeTokenStreamException();
			return _tokens[_pos];
		}

		public int Position
		{
			get => _pos;
			set
			{
				if (value < 0 || value > _tokens.Count) throw new ArgumentOutOfRangeException();
				_pos = value;
			}
		}

		public void Add(CodeToken token)
		{
			_tokens.Add(token);
		}

		public void Add(CodeTokenStream stream)
		{
			if (stream != null) _tokens.AddRange(stream._tokens);
		}

		public void Add(IEnumerable<CodeToken> tokens)
		{
			if (tokens != null) _tokens.AddRange(tokens);
		}

		#region IEnumerable<CodeToken>
		public IEnumerator<CodeToken> GetEnumerator()
		{
			return new CodeTokenEnumerator(this);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new CodeTokenEnumerator(this);
		}

		public class CodeTokenEnumerator : IEnumerator<CodeToken>
		{
			private CodeTokenStream _stream;
			private int _pos = -1;

			public CodeTokenEnumerator(CodeTokenStream stream)
			{
				_stream = stream ?? throw new ArgumentNullException(nameof(stream));
			}

			public CodeToken Current
			{
				get
				{
					if (_pos < 0 || _pos >= _stream._tokens.Count) throw new InvalidOperationException("No current code token.");
					return _stream._tokens[_pos];
				}
			}

			object System.Collections.IEnumerator.Current
			{
				get
				{
					if (_pos < 0 || _pos >= _stream._tokens.Count) throw new InvalidOperationException("No current code token.");
					return _stream._tokens[_pos];
				}
			}

			public bool MoveNext()
			{
				if (_pos + 1 < _stream._tokens.Count)
				{
					_pos++;
					return true;
				}
				return false;
			}

			public void Reset()
			{
				_pos = 0;
			}

			public void Dispose()
			{
			}
		}
		#endregion
	}

	internal class EndOfCodeTokenStreamException : Exception
	{
		public EndOfCodeTokenStreamException() { }
	}
}
