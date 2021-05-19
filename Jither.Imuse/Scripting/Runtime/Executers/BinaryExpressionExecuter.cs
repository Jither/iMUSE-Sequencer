using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class BinaryExpressionExecuter : ExpressionExecuter
    {
        private readonly ExpressionExecuter left;
        private readonly ExpressionExecuter right;
        private readonly BinaryOperator op;

        public BinaryExpressionExecuter(BinaryExpression expr) : base(expr)
        {
            left = Build(expr.Left);
            right = Build(expr.Right);
            op = expr.Operator;
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            RuntimeValue result;

            if (op == BinaryOperator.And || op == BinaryOperator.Or)
            {
                // These operators are short-circuiting - don't evaluate right until we know the value of left:
                result = op switch
                {
                    BinaryOperator.And => left.Execute(context).AsBoolean(left) && right.Execute(context).AsBoolean(right) ? BooleanValue.True : BooleanValue.False,
                    BinaryOperator.Or => left.Execute(context).AsBoolean(left) || right.Execute(context).AsBoolean(right) ? BooleanValue.True : BooleanValue.False,
                    _ => throw new NotImplementedException($"Binary operator {op} not implemented in interpreter"),
                };
            }
            else if (op == BinaryOperator.Equal || op == BinaryOperator.NotEqual)
            {
                // These operate on any type:
                result = op switch
                {
                    BinaryOperator.Equal => left.Execute(context).IsEqualTo(right.Execute(context)) ? BooleanValue.True : BooleanValue.False,
                    BinaryOperator.NotEqual => left.Execute(context).IsEqualTo(right.Execute(context)) ? BooleanValue.False : BooleanValue.True,
                    _ => throw new NotImplementedException($"Binary operator {op} not implemented in interpreter")
                };
            }
            else
            {
                // These operate only on integers
                var leftValue = left.Execute(context).AsInteger(left);
                var rightValue = right.Execute(context).AsInteger(right);

                result = op switch
                {
                    BinaryOperator.Add => IntegerValue.Create(leftValue + rightValue),
                    BinaryOperator.Subtract => IntegerValue.Create(leftValue - rightValue),
                    BinaryOperator.Multiply => IntegerValue.Create(leftValue * rightValue),
                    BinaryOperator.Divide => IntegerValue.Create(leftValue / rightValue),
                    BinaryOperator.Modulo => IntegerValue.Create(leftValue % rightValue),
                    BinaryOperator.Greater => leftValue > rightValue ? BooleanValue.True : BooleanValue.False,
                    BinaryOperator.GreaterOrEqual => leftValue >= rightValue ? BooleanValue.True : BooleanValue.False,
                    BinaryOperator.Less => leftValue < rightValue ? BooleanValue.True : BooleanValue.False,
                    BinaryOperator.LessOrEqual => leftValue <= rightValue ? BooleanValue.True : BooleanValue.False,
                    _ => throw new NotImplementedException($"Binary operator {op} not implemented in interpreter"),
                };
            }

            return result;
        }
    }
}
