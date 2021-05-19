using System;
using System.Collections.Generic;
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
}
