using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class DefineDeclarationExecuter : DeclarationExecuter
    {
        private readonly string name;
        private readonly ExpressionExecuter value;

        public DefineDeclarationExecuter(DefineDeclaration define) : base(define)
        {
            name = define.Identifier.Name;
            value = ExpressionExecuter.Build(define.Value);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var valueResult = value.GetValue(context);
            context.CurrentScope.AddSymbol(name, valueResult, isConstant: true);

            return ExecutionResult.Void;
        }
    }
}
