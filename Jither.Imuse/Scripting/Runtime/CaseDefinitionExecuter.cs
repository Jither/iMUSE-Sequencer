using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime
{
    public class CaseDefinitionExecuter : Executer
    {
        private readonly LiteralExecuter test;
        private readonly StatementExecuter consequent;

        public CaseDefinitionExecuter(CaseDefinition definition)
        {
            if (test != null)
            {
                test = new LiteralExecuter(definition.Test);
            }

            consequent = StatementExecuter.Build(definition.Consequent);
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }

}
