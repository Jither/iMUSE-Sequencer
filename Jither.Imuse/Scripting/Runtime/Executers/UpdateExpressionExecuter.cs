using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class UpdateExpressionExecuter : ExpressionExecuter
    {
        private readonly Identifier identifier;
        private readonly UpdateOperator op;

        public UpdateExpressionExecuter(UpdateExpression expr) : base(expr)
        {
            identifier = expr.Argument;
            op = expr.Operator;
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var symbol = context.CurrentScope.GetSymbol(identifier, identifier.Name);
            var value = symbol.Value.AsInteger(identifier);
            switch (op)
            {
                case UpdateOperator.Increment:
                    value++;
                    break;
                case UpdateOperator.Decrement:
                    value--;
                    break;
                default:
                    throw new NotImplementedException($"Update operator {op} not implemented");
            }
            var newValue = IntegerValue.Create(value);
            symbol.Update(Node, newValue);
            return new ExecutionResult(ExecutionResultType.Normal, newValue);
        }
    }
}
