using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class AssignmentExpression : Expression
    {
        public override NodeType Type => NodeType.AssignmentStatement;
        public Identifier Left { get; }
        public Expression Right { get; }
        public AssignmentOperator Operator { get; }

        public AssignmentExpression(Identifier left, Expression right, AssignmentOperator op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Left;
                yield return Right;
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitAssignmentExpression(this);
    }
}
