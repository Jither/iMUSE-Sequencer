using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class ExpressionStatement : Statement
    {
        public override NodeType Type => NodeType.ExpressionStatement;
        public Expression Expression { get; }

        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Expression;
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitExpressionStatement(this);
    }
}
