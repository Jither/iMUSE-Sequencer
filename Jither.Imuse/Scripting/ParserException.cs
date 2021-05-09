using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting
{
    public class ParserException : Exception
    {
        public SourceRange Range { get; }

        public override string Message => $"{base.Message} at {Range.Start}";

        public ParserException(string message, SourceRange range) : base(message)
        {
            Range = range;
        }
    }

    public class ScannerException : ParserException
    {
        public ScannerException(string message, SourceLocation start) : base(message, new SourceRange(start, start))
        {
        }
    }

    public class UnexpectedTokenException : ParserException
    {
        public UnexpectedTokenException(Token token, string message) : base(message, token.Range)
        {
        }
    }

    public class InvalidTokenException : ParserException
    {
        public InvalidTokenException(string message, SourceRange range) : base(message, range)
        {
        }
    }
}
