using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class ActionDeclarationExecuter : DeclarationExecuter
    {
        private readonly string name;
        private readonly ExpressionExecuter during;
        private readonly StatementExecuter body;

        public ActionDeclarationExecuter(ActionDeclaration action) : base(action)
        {
            name = action.Name?.Name;
            during = ExpressionExecuter.Build(action.During);
            body = StatementExecuter.Build(action.Body);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            int? duringValue = during?.GetValue(context).AsInteger(this);
            var result = new ActionValue(new ImuseAction(name, duringValue, body));
            if (name != null)
            {
                context.CurrentScope.AddOrUpdateSymbol(this.Node, name, result);
            }
            return new ExecutionResult(ExecutionResultType.Normal, result);
        }
    }

}
