using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class ForStatementExecuter : StatementExecuter
    {
        private readonly IdentifierExecuter iterator;
        private readonly ExpressionExecuter from;
        private readonly ExpressionExecuter to;
        private readonly StatementExecuter body;
        private readonly bool increment;

        public ForStatementExecuter(ForStatement stmt)
        {
            iterator = new IdentifierExecuter(stmt.Iterator);
            from = ExpressionExecuter.Build(stmt.From);
            to = ExpressionExecuter.Build(stmt.To);
            body = Build(stmt.Body);
            increment = stmt.Increment;
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
