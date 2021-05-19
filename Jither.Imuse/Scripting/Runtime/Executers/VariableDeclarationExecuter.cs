using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class VariableDeclarationExecuter : DeclarationExecuter
    {
        private readonly string name;
        private readonly ExpressionExecuter value;

        public VariableDeclarationExecuter(VariableDeclaration variable) : base(variable)
        {
            name = variable.Identifier.Name;
            if (variable.Value != null)
            {
                value = ExpressionExecuter.Build(variable.Value);
            }
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            var valueResult = value?.Execute(context) ?? RuntimeValue.Null;
            context.CurrentScope.AddSymbol(name, valueResult);

            return RuntimeValue.Void;
        }
    }
}
