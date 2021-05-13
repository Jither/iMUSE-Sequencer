using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class CallExpressionExecuter : ExpressionExecuter
    {
        private readonly Identifier identifier;
        private readonly List<ExpressionExecuter> arguments = new();

        public CallExpressionExecuter(CallExpression expr) : base(expr)
        {
            identifier = expr.Name;
            foreach (var argument in expr.Arguments)
            {
                arguments.Add(Build(argument));
            }
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var argValues = new List<RuntimeValue>();
            foreach (var arg in arguments)
            {
                argValues.Add(arg.GetValue(context));
            }

            var cmdSymbol = context.CurrentScope.GetSymbol(identifier, identifier.Name);

            var result = cmdSymbol.Value.AsCommand(identifier).Execute(argValues);

            return new ExecutionResult(ExecutionResultType.Normal, result);
        }
    }
}
