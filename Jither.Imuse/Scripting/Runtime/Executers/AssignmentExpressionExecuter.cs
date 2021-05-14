using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class AssignmentExpressionExecuter : ExpressionExecuter
    {
        private readonly Identifier identifier;
        private readonly string identifierName;
        private readonly ExpressionExecuter right;
        private readonly AssignmentOperator op;

        public AssignmentExpressionExecuter(AssignmentExpression expr) : base(expr)
        {
            identifier = expr.Left;
            identifierName = expr.Left.Name;
            right = Build(expr.Right);
            op = expr.Operator;
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var rightValue = right.GetValue(context);
            RuntimeValue value;
            if (op == AssignmentOperator.Equals)
            {
                value = rightValue;
            }
            else
            {
                var leftSymbol = context.CurrentScope.GetSymbol(this.identifier, identifierName);
                var leftInt = leftSymbol.Value.AsInteger(this);
                var rightInt = rightValue.AsInteger(this);
                var resultInt = op switch
                {
                    AssignmentOperator.Add => leftInt + rightInt,
                    AssignmentOperator.Subtract => leftInt - rightInt,
                    AssignmentOperator.Multiply => leftInt * rightInt,
                    AssignmentOperator.Divide => leftInt / rightInt,
                    AssignmentOperator.Modulo => leftInt % rightInt,
                    _ => throw new NotImplementedException($"Assignment operator {op} not implemented in interpreter"),
                };
                value = IntegerValue.Create(resultInt);
            }
            context.CurrentScope.AddOrUpdateSymbol(this.Node, identifierName, value);
            return new ExecutionResult(ExecutionResultType.Normal, value);
        }
    }

}
