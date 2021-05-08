using System.Collections.Generic;
using Jither.Imuse.Scripting;

namespace Jither.Imuse.Scripting.Ast
{
    public abstract class Node
    {
        public Range Range { get; private set; }

        // For ensuring integrity of nodes
        internal bool finalized;

        public abstract NodeType Type { get; }

        protected Node()
        {
        }

        public void Finalize(Location start, Location end)
        {
            Range = new Range(start, end);
            finalized = true;
        }

        public abstract IEnumerable<Node> Children { get; }
        public abstract void Accept(IAstVisitor visitor);
    }
}
