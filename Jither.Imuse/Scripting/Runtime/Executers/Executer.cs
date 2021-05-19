using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public abstract class Executer
    {
        public Node Node { get; }

        protected Executer(Node node)
        {
            this.Node = node;
        }

        public abstract RuntimeValue Execute(ExecutionContext context);
    }
}
