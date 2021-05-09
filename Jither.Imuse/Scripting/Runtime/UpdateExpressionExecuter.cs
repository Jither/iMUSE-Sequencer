using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class UpdateExpressionExecuter : ExpressionExecuter
    {
        private readonly ExpressionExecuter argument;
        private readonly UpdateOperator op;
        private readonly bool prefix;

        public UpdateExpressionExecuter(UpdateExpression expr)
        {
            argument = Build(expr.Argument);
            op = expr.Operator;
            prefix = expr.Prefix;
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
