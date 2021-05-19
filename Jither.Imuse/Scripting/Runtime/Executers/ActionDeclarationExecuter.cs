using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class ActionDeclarationExecuter : DeclarationExecuter
    {
        private readonly string name;
        private readonly ExpressionExecuter during;
        private readonly BlockStatementExecuter body;

        public ActionDeclarationExecuter(ActionDeclaration action) : base(action)
        {
            name = action.Name?.Name;
            if (during != null)
            {
                during = ExpressionExecuter.Build(action.During);
            }
            body = new BlockStatementExecuter(action.Body);
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            int? duringValue = during?.Execute(context).AsInteger(this);
            var result = new ActionValue(new ImuseAction(name, duringValue, body));
            if (name != null)
            {
                context.CurrentScope.AddOrUpdateSymbol(this.Node, name, result);
            }
            return result;
        }
    }

}
