using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using Jither.Utilities;
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
            var cmdSymbol = context.CurrentScope.GetSymbol(identifier, identifier.Name);

            var command = cmdSymbol.Value.AsCommand(identifier);

            var argValues = new List<RuntimeValue>();

            var parameters = command.Parameters;
            int parameterIndex = 0;
            int argumentIndex = 0;
            while (argumentIndex < arguments.Count)
            {
                var arg = arguments[argumentIndex++];
                if (parameterIndex >= parameters.Count)
                {
                    ErrorHelper.ThrowArgumentError(Node, "Too many arguments", command);
                }
                var param = parameters[parameterIndex++];

                // Allow named arguments (e.g. jump-to sound 1 track 2)
                if (arg is IdentifierExecuter ident && ident.Name == param.Name)
                {
                    arg = arguments[argumentIndex++];
                }

                var value = arg.GetValue(context);

                // Argument type checking
                if (value.Type != param.Type)
                {
                    ErrorHelper.ThrowArgumentError(arg.Node, $"Incorrect type ({value.Type.GetFriendlyName()}) for parameter {param.Name} ({param.Type.GetFriendlyName()})", command);
                }
                argValues.Add(value);
            }

            if (argValues.Count != command.Parameters.Count)
            {
                ErrorHelper.ThrowArgumentError(Node, "Too few arguments", command);
            }

            var result = command.Execute(argValues);

            return new ExecutionResult(ExecutionResultType.Normal, result);
        }
    }
}
