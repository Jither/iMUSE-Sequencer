using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public abstract class Node
    {
        public SourceRange Range { get; private set; }

        // For ensuring integrity of nodes
        internal bool finalized;

        public abstract NodeType Type { get; }

        protected Node()
        {
        }

        public void Finalize(SourceLocation start, SourceLocation end)
        {
            Range = new SourceRange(start, end);
            finalized = true;
        }

        public abstract IEnumerable<Node> Children { get; }
        public abstract void Accept(IAstVisitor visitor);
    }
}
