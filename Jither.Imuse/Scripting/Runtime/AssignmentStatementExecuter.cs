using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Runtime
{
    public class AssignmentStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter left;
        private readonly ExpressionExecuter right;
        private readonly AssignmentOperator op;

        public AssignmentStatementExecuter(AssignmentStatement stmt)
        {
            left = ExpressionExecuter.Build(stmt.Left);
            right = ExpressionExecuter.Build(stmt.Right);
            op = stmt.Operator;
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }

}
