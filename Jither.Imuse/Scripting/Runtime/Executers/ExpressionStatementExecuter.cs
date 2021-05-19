using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
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

        public override RuntimeValue Execute(ExecutionContext context)
        {
            expression.Execute(context);
            return RuntimeValue.Void;
        }
    }
}
