using System;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting.Ast
{
    /// <summary>
    /// Pseudo statement, only used during the process of flattening syntax tree.
    /// </summary>
    public class Label : Statement
    {
        public override NodeType Type => NodeType.Label;

        public override IEnumerable<Node> Children => Enumerable.Empty<Node>();

        private readonly List<JumpStatement> references = new();

        public void AddReference(JumpStatement stmt)
        {
            references.Add(stmt);
        }

        public void AssignIndex(int index)
        {
            foreach (var reference in references)
            {
                reference.Destination = index;
            }
        }

        public override void Accept(IAstVisitor visitor)
        {
            throw new NotImplementedException("Label is a pseudo-statement - should never be visited");
        }
    }
}
