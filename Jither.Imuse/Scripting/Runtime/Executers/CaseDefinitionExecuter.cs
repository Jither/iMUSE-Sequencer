using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class CaseDefinitionExecuter : Executer
    {
        private readonly LiteralExecuter test;
        private readonly StatementExecuter consequent;

        public CaseDefinitionExecuter(CaseDefinition definition) : base(definition)
        {
            if (test != null)
            {
                test = new LiteralExecuter(definition.Test);
            }

            consequent = StatementExecuter.Build(definition.Consequent);
        }

        public bool IsMatch(ExecutionContext context, RuntimeValue discriminantValue)
        {
            if (test == null)
            {
                // Default case
                return true;
            }
            var testValue = test.GetValue(context);
            return (testValue == discriminantValue);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            return consequent.Execute(context);
        }
    }

}
