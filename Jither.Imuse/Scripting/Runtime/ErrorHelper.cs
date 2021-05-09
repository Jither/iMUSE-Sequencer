using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Runtime
{
    public class ScriptException : Exception
    {
        public Node Node { get; }

        public override string Message => $"{base.Message} at {Node.Range.Start}";

        public ScriptException(Node node, string message) : base(message)
        {
            Node = node;
        }
    }
    
    public static class ErrorHelper
    {
        [DoesNotReturn]
        public static void ThrowUnknownNode(Node node)
        {
            throw new ScriptException(node, $"Unknown node: {node.Type}");
        }

        [DoesNotReturn]
        public static T ThrowUnknownNode<T>(Node node) where T: Executer
        {
            throw new ScriptException(node, $"Unknown node: {node.Type}");
        }
    }
}
