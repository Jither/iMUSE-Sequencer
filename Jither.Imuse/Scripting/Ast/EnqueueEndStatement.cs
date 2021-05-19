using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting.Ast
{
    public class EnqueueEndStatement : Statement
    {
        public override NodeType Type => NodeType.EnqueueEndStatement;

        public override IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitEnqueueEndStatement(this);
        }
    }
}
