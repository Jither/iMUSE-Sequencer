using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class ExpressionStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter expression;

        public ExpressionStatementExecuter(ExpressionStatement stmt) : base(stmt)
        {
            expression = ExpressionExecuter.Build(stmt.Expression);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            expression.Execute(context);
            return ExecutionResult.Void;
        }
    }
}
