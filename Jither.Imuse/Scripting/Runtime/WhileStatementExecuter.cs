using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class WhileStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter test;
        private readonly StatementExecuter body;

        public WhileStatementExecuter(WhileStatement stmt)
        {
            test = ExpressionExecuter.Build(stmt.Test);
            body = Build(stmt.Body);
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }

}
