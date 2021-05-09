using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class DoStatementExecuter : StatementExecuter
    {
        private readonly StatementExecuter body;
        private readonly ExpressionExecuter test;

        public DoStatementExecuter(DoStatement doStmt)
        {
            body = Build(doStmt.Body);
            test = ExpressionExecuter.Build(doStmt.Test);
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }

}
