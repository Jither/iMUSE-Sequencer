using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class IfStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter test;
        private readonly StatementExecuter consequent;
        private readonly StatementExecuter alternate;

        public IfStatementExecuter(IfStatement stmt)
        {
            test = ExpressionExecuter.Build(stmt.Test);
            consequent = Build(stmt.Consequent);
            if (stmt.Alternate != null)
            {
                alternate = Build(stmt.Alternate);
            }
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
