using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class UnaryExpression : Expression
    {
        public override NodeType Type => NodeType.UnaryExpression;
        public UnaryOperator Operator { get; }
        public Expression Argument { get; }

        public UnaryExpression(Expression argument, UnaryOperator op)
        {
            Argument = argument;
            Operator = op;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Argument;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitUnaryExpression(this);
    }

    public enum UpdateOperator
    {
        Increment,
        Decrement
    }

    public class UpdateExpression : Expression
    {
        public override NodeType Type => NodeType.UpdateExpression;
        public UpdateOperator Operator { get; }
        public Expression Argument { get; }
        public bool Prefix { get; }

        public UpdateExpression(Expression argument, UpdateOperator op, bool prefix)
        {
            Argument = argument;
            Operator = op;
            Prefix = prefix;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Argument;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitUpdateExpression(this);
    }
}
