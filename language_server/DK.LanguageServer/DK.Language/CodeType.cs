using System;
using System.Collections.Generic;
using System.Text;

namespace DK.Language
{
	public enum CodeType
	{
		None,
		WhiteSpace,
		Comment,
		Word,
		Chain,
		Number,
		StringLiteral,
		Equal,
		NotEqual,
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulus,
		Assign,
		AddAssign,
		SubractAssign,
		MultiplyAssign,
		DivideAssign,
		ModulusAssign,
		OpenBrace,
		CloseBrace,
		OpenBracket,
		CloseBracket,
		OpenArray,
		CloseArray,
		Comma,
		Dot,
		Preprocessor,
		PreprocessorArgument,
		Invalid
	}
}
