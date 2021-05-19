using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting.Ast
{
    public class JumpStatement : Statement
    {
        public override NodeType Type => NodeType.JumpStatement;

        public override IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public int Destination { get; internal set; }

        public JumpStatement(Label destination)
        {
            destination.AddReference(this);
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitJumpStatement(this);
        }
    }
}
