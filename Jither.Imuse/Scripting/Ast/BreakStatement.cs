using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting.Ast
{
    public class BreakStatement : Statement
    {
        public override NodeType Type => NodeType.BreakStatement;
        
        public BreakStatement()
        {

        }

        public override IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public override void Accept(IAstVisitor visitor) => visitor.VisitBreakStatement(this);
    }
}
