using Jither.Imuse.Scripting.Ast;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime
{
    public class CallStatementExecuter : StatementExecuter
    {
        private readonly IdentifierExecuter name;
        private readonly List<ExpressionExecuter> arguments = new();
        
        public CallStatementExecuter(CallStatement call)
        {
            name = new IdentifierExecuter(call.Name);
            foreach (var argument in call.Arguments)
            {
                arguments.Add(ExpressionExecuter.Build(argument));
            }
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }

}
