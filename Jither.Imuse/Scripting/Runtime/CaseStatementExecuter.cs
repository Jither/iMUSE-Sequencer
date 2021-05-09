using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime
{
    public class CaseStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter discriminant;
        private readonly List<CaseDefinitionExecuter> cases = new();

        public CaseStatementExecuter(CaseStatement stmt)
        {
            discriminant = ExpressionExecuter.Build(stmt.Discriminant);
            foreach (var c in stmt.Cases)
            {
                cases.Add(new CaseDefinitionExecuter(c));
            }
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }

}
