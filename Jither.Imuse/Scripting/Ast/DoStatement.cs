using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Ast
{
    public class DoStatement : Statement
    {
        public override NodeType Type => NodeType.DoStatement;
        public Statement Body { get; }
        public Expression Test { get; }

        public DoStatement(Statement body, Expression test)
        {
            Body = body;
            Test = test;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Body;
                yield return Test;
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitDoStatement(this);
    }
}
