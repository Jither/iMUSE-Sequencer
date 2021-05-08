using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class IfStatement : Statement
    {
        public override NodeType Type => NodeType.IfStatement;
        public Expression Condition { get; }
        public List<Statement> Consequent { get; }
        public List<Statement> Alternate { get; }

        public IfStatement(Expression condition, List<Statement> consequent, List<Statement> alternate)
        {
            Condition = condition;
            Consequent = consequent;
            Alternate = alternate;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Condition;
                foreach (var stmt in Consequent)
                {
                    yield return stmt;
                }
                if (Alternate != null)
                {
                    foreach (var stmt in Alternate)
                    {
                        yield return stmt;
                    }
                }
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitIfStatement(this);
    }
}
