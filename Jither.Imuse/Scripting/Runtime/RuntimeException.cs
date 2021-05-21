using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime
{
    public class RuntimeException : ScriptException
    {
        public Node Node { get; }
        public override SourceRange Range => Node.Range;

        public override string Message => $"Runtime error at {Node.Range.Start}: {base.Message}";

        public RuntimeException(Node node, string message) : base(message)
        {
            Node = node;
        }
    }
}
