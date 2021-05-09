using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Ast
{
    public class WhileStatement : Statement
    {
        public override NodeType Type => NodeType.WhileStatement;
        public Expression Test { get; }
        public Statement Body { get; }

        public WhileStatement(Expression test, Statement body)
        {
            Test = test;
            Body = body;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Test;
                yield return Body;
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitWhileStatement(this);
    }

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
