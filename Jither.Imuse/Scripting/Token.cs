using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
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
        public Time TimeValue;

        public Token(TokenType type, string value, SourceRange range)
        {
            Type = type;
            Value = value;
            Range = range;

            if (type == TokenType.IntegerLiteral)
            {
                // Negative numbers are parsed as UnaryExpression (minus + literal), so no leading signs here
                if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var integer))
                {
                    IntegerValue = integer;
                }
                else
                {
                    throw new ParserException($"Invalid numeric literal: {value}", range);
                }
            }
            else if (type == TokenType.TimeLiteral)
            {
                if (Time.TryParse(value, out var time))
                {
                    TimeValue = time;
                }
                else
                {
                    throw new ParserException($"Invalid time literal: {value}", range);
                }
            }
        }

        public override string ToString()
        {
            return $"{Range.Start,-6} {Type,-30} {Value}";
        }
    }
}
