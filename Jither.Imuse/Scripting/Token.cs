using Jither.Imuse.Scripting.Ast;
using System;
using System.Globalization;

namespace Jither.Imuse.Scripting
{
    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        public SourceRange Range { get; }

        public int IntegerValue;
        public double NumericValue;

        public Token(TokenType type, string value, SourceRange range)
        {
            Type = type;
            Value = value;
            Range = range;

            if (type == TokenType.NumericLiteral)
            {
                // Negative numbers are parsed as UnaryExpression (minus + literal), so no leading signs here
                if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var integer))
                {
                    IntegerValue = integer;
                    Type = TokenType.IntegerLiteral;
                }
                else if (double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var floating))
                {
                    NumericValue = floating;
                }
                else
                {
                    throw new ParserException($"Invalid numeric literal: {value}", range);
                }
            }
        }

        public override string ToString()
        {
            return $"{Range.Start,-6} {Type,-30} {Value}";
        }
    }
}
