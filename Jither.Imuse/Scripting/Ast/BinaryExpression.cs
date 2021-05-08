using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class BinaryExpression : Expression
    {
        public override NodeType Type => NodeType.BinaryExpression;
        public Expression Left { get; }
        public Expression Right { get; }
        public BinaryOperator Operator { get; }

        public BinaryExpression(Expression left, Expression right, BinaryOperator op)
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

        public override void Accept(IAstVisitor visitor) => visitor.VisitBinaryExpression(this);
    }
}
