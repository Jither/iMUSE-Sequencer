using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting.Ast
{
    public class BreakHereStatement : Statement
    {
        public override NodeType Type => NodeType.BreakHereStatement;
        
        public Expression Count { get; }

        public BreakHereStatement(Expression count)
        {
            Count = count;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                if (Count != null)
                {
                    yield return Count;
                }
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitBreakHereStatement(this);
    }
}
