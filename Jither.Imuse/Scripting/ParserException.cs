using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting
{
    public abstract class ScriptException : Exception
    {
        protected ScriptException(string message) : base(message)
        {
        }

        public abstract SourceRange Range { get; }
    }

    public class ParserException : ScriptException
    {
        public override SourceRange Range { get; }

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

    public class SyntaxException : ParserException
    {
        public SyntaxException(Token token, string message) : base(message, token.Range)
        {
        }
    }
}
