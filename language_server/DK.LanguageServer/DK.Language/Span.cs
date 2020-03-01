using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	public class Span
	{
		private int _start;
		private int _length;

		public static readonly Span Empty = new Span(0, 0);

		public Span(int start, int length)
		{
			if (start < 0) throw new ArgumentOutOfRangeException(nameof(start));
			if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
			_start = start;
			_length = length;
		}

		public static Span FromPosition(int a)
		{
			return new Span(a, 0);
		}

		public static Span FromPosition(int a, int b)
		{
			if (a <= b) return new Span(a, b - a);
			else return new Span(b, a - b);
		}

		public int Start
		{
			get { return _start; }
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException();
				_start = value;
			}
		}

		public int Length
		{
			get { return _length; }
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException();
				_length = value;
			}
		}

		public int End
		{
			get { return _start + _length; }
			set
			{
				if (value < _start) throw new ArgumentOutOfRangeException();
				_length = value - _start;
			}
		}

		public bool IsEmpty => _length == 0;

		public override string ToString() => $"[{_start}-{_start + _length}]";
	}
}
