using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class UnaryExpressionExecuter : ExpressionExecuter
    {
        private readonly ExpressionExecuter argument;
        private readonly UnaryOperator op;

        public UnaryExpressionExecuter(UnaryExpression expr) : base(expr)
        {
            argument = Build(expr.Argument);
            op = expr.Operator;
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            RuntimeValue result = op switch
            {
                UnaryOperator.Plus => IntegerValue.Create(argument.GetValue(context).AsInteger(argument)),
                UnaryOperator.Minus => IntegerValue.Create(-argument.GetValue(context).AsInteger(argument)),
                UnaryOperator.Not => argument.GetValue(context).AsBoolean(argument) ? BooleanValue.False : BooleanValue.True,
                _ => throw new NotImplementedException($"Unary operator {op} not implemented."),
            };
            return new ExecutionResult(ExecutionResultType.Normal, result);
        }
    }
}
