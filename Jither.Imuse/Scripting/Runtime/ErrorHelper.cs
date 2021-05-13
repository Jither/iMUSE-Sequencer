using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Runtime.Executers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Runtime
{
    public static class ErrorHelper
    {
        [DoesNotReturn]
        public static void ThrowUnknownNode(Node node)
        {
            throw new RuntimeException(node, $"Unknown node: {node.Type}");
        }

        [DoesNotReturn]
        public static T ThrowUnknownNode<T>(Node node) where T: Executer
        {
            throw new RuntimeException(node, $"Unknown node: {node.Type}");
        }

        [DoesNotReturn]
        public static void ThrowTypeError(Node node, string message)
        {
            throw new RuntimeException(node, message);
        }

    }
}
