using System;

namespace Jither.Imuse.Scripting
{
    public class ParserException : Exception
    {
        public Range Range { get; }

        public override string Message => $"{base.Message} at {Range.Start}";

        public ParserException(string message, Range range) : base(message)
        {
            Range = range;
        }
    }

    public class ScannerException : ParserException
    {
        public ScannerException(string message, Location start) : base(message, new Range(start, start))
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
        public InvalidTokenException(string message, Range range) : base(message, range)
        {
        }
    }
}
