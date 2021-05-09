using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime
{
    public class DefineDeclarationExecuter : DeclarationExecuter
    {
        private readonly IdentifierExecuter identifier;
        private readonly ExpressionExecuter value;

        public DefineDeclarationExecuter(DefineDeclaration define)
        {
            identifier = new IdentifierExecuter(define.Identifier);
            value = ExpressionExecuter.Build(define.Value);
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
