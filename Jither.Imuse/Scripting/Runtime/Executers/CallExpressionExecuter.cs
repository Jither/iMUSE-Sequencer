using Jither.Imuse.Commands;
using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using Jither.Utilities;
using System;
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

            // Argument checks
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
                    ErrorHelper.ThrowArgumentError(arg.Node, $"Incorrect type ({value.Type.GetDisplayName()}) for parameter {param.Name} ({param.Type.GetDisplayName()})", command);
                }
                argValues.Add(value);
            }

            if (argValues.Count != command.Parameters.Count)
            {
                ErrorHelper.ThrowArgumentError(Node, "Not enough arguments", command);
            }

            RuntimeValue result;
            if (context.EnqueuingCommands != null && command.IsEnqueuable)
            {
                context.EnqueuingCommands.Add(new CommandCall(command, argValues));
                result = RuntimeValue.Void;
            }
            else
            {
                result = command.Execute(argValues);
            }

            return new ExecutionResult(ExecutionResultType.Normal, result);
        }
    }
}
