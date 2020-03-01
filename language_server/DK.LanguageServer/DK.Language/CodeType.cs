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
		Suppressed,
		Word,
		Chain,
		Number,
		StringLiteral,
		Equal,
		NotEqual,
		And,
		Or,
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulus,
		Assign,
		AddAssign,
		SubtractAssign,
		MultiplyAssign,
		DivideAssign,
		ModulusAssign,
		Less,
		LessEqual,
		Greater,
		GreaterEqual,
		OpenBrace,
		CloseBrace,
		OpenBracket,
		CloseBracket,
		OpenArray,
		CloseArray,
		Comma,
		Dot,
		Ternary,
		Colon,
		Semicolon,

		Preprocessor,
		PreprocessorArgument,
		MacroName,
		LineContinue,

		Argument,
		Variable,

		Invalid
	}

	public static class CodeTypeExtensions
	{
		public static CodeType GetClosingType(this CodeType type)
		{
			switch (type)
			{
				case CodeType.OpenArray:
					return CodeType.CloseArray;
				case CodeType.OpenBrace:
					return CodeType.CloseBrace;
				case CodeType.OpenBracket:
					return CodeType.CloseBracket;
				default:
					return CodeType.None;
			}
		}

		public static int GetPrecedence(this CodeType type)
		{
			// Even numbers are left-to-right associativity; odd numbers are right-to-left.

			switch (type)
			{
				case CodeType.Multiply:
				case CodeType.Divide:
				case CodeType.Modulus:
					return 12;

				case CodeType.Add:
				case CodeType.Subtract:
					return 10;

				case CodeType.Less:
				case CodeType.LessEqual:
				case CodeType.Greater:
				case CodeType.GreaterEqual:
					return 8;

				case CodeType.Equal:
				case CodeType.NotEqual:
					return 6;

				case CodeType.And:
				case CodeType.Or:
					return 4;

				case CodeType.Ternary:
					return 2;

				case CodeType.Assign:
				case CodeType.AddAssign:
				case CodeType.SubtractAssign:
				case CodeType.MultiplyAssign:
				case CodeType.DivideAssign:
				case CodeType.ModulusAssign:
					return 1;

				default:
					return 0;
			}
		}
	}
}
