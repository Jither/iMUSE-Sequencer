using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class UnaryExpressionExecuter : ExpressionExecuter
    {
        private readonly ExpressionExecuter argument;
        private readonly UnaryOperator op;

        public UnaryExpressionExecuter(UnaryExpression expr)
        {
            argument = Build(expr.Argument);
            op = expr.Operator;
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
