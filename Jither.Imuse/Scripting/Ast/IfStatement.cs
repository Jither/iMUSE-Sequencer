using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class IfStatement : Statement
    {
        public override NodeType Type => NodeType.IfStatement;
        public Expression Test { get; }
        public Statement Consequent { get; }
        public Statement Alternate { get; }

        public IfStatement(Expression test, Statement consequent, Statement alternate)
        {
            Test = test;
            Consequent = consequent;
            Alternate = alternate;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Test;
                yield return Consequent;
                if (Alternate != null)
                {
                    yield return Alternate;
                }
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitIfStatement(this);
    }
}
