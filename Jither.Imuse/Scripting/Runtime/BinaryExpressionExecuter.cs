using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class BinaryExpressionExecuter : ExpressionExecuter
    {
        private readonly ExpressionExecuter left;
        private readonly ExpressionExecuter right;

        public BinaryExpressionExecuter(BinaryExpression expr)
        {
            left = Build(expr.Left);
            right = Build(expr.Right);
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
