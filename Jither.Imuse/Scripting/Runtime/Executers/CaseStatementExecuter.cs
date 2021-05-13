using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class CaseStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter discriminant;
        private readonly List<CaseDefinitionExecuter> cases = new();

        public CaseStatementExecuter(CaseStatement stmt) : base(stmt)
        {
            discriminant = ExpressionExecuter.Build(stmt.Discriminant);
            foreach (var c in stmt.Cases)
            {
                cases.Add(new CaseDefinitionExecuter(c));
            }
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var discriminantValue = discriminant.GetValue(context);
            foreach (var c in cases)
            {
                if (c.IsMatch(context, discriminantValue))
                {
                    return c.Execute(context);
                }
            }
            return ExecutionResult.Void;
        }
    }

}
