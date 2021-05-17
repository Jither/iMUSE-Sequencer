﻿using Jither.Imuse.Scripting.Ast;
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

        public override ExecutionResult Execute(ExecutionContext context)
        {
            RuntimeValue result;

            if (op == BinaryOperator.And || op == BinaryOperator.Or)
            {
                // These operators are short-circuiting - don't evaluate right until we know the value of left:
                result = op switch
                {
                    BinaryOperator.And => left.GetValue(context).AsBoolean(left) && right.GetValue(context).AsBoolean(right) ? BooleanValue.True : BooleanValue.False,
                    BinaryOperator.Or => left.GetValue(context).AsBoolean(left) || right.GetValue(context).AsBoolean(right) ? BooleanValue.True : BooleanValue.False,
                    _ => throw new NotImplementedException($"Binary operator {op} not implemented in interpreter"),
                };
            }
            else if (op == BinaryOperator.Equal || op == BinaryOperator.NotEqual)
            {
                // These operate on any type:
                result = op switch
                {
                    BinaryOperator.Equal => left.GetValue(context).IsEqualTo(right.GetValue(context)) ? BooleanValue.True : BooleanValue.False,
                    BinaryOperator.NotEqual => left.GetValue(context).IsEqualTo(right.GetValue(context)) ? BooleanValue.False : BooleanValue.True,
                    _ => throw new NotImplementedException($"Binary operator {op} not implemented in interpreter")
                };
            }
            else
            {
                // These operate only on integers
                var leftValue = left.GetValue(context).AsInteger(left);
                var rightValue = right.GetValue(context).AsInteger(right);

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

            return new ExecutionResult(ExecutionResultType.Normal, result);
        }
    }
}