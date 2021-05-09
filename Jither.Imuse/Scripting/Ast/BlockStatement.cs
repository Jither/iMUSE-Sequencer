using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Ast
{
    public class BlockStatement : Statement
    {
        public override NodeType Type => NodeType.BlockStatement;
        
        public List<Statement> Body { get; }

        public BlockStatement(List<Statement> body)
        {
            Body = body;
        }

        public override IEnumerable<Node> Children => Body;

        public override void Accept(IAstVisitor visitor) => visitor.VisitBlockStatement(this);
    }
}
