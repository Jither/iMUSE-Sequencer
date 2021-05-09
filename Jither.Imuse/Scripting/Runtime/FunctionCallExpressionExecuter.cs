using Jither.Imuse.Scripting.Ast;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime
{
    public class FunctionCallExpressionExecuter : ExpressionExecuter
    {
        private readonly IdentifierExecuter name;
        private readonly List<ExpressionExecuter> arguments = new();
        public FunctionCallExpressionExecuter(FunctionCallExpression expr)
        {
            name = new IdentifierExecuter(expr.Name);
            foreach (var argument in expr.Arguments)
            {
                arguments.Add(Build(argument));
            }
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
