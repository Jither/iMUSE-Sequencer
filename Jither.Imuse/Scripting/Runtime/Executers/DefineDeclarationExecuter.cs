using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
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

        public override RuntimeValue Execute(ExecutionContext context)
        {
            var valueResult = value.Execute(context);
            context.CurrentScope.AddSymbol(name, valueResult, isConstant: true);

            return RuntimeValue.Void;
        }
    }
}
