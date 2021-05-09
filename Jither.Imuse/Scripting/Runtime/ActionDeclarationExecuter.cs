using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime
{
    public class ActionDeclarationExecuter : DeclarationExecuter
    {
        private readonly IdentifierExecuter name;
        private readonly ExpressionExecuter during;
        private readonly StatementExecuter body;

        public ActionDeclarationExecuter(ActionDeclaration action)
        {
            name = new IdentifierExecuter(action.Name);
            during = ExpressionExecuter.Build(action.During);
            body = StatementExecuter.Build(action.Body);
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }

}
