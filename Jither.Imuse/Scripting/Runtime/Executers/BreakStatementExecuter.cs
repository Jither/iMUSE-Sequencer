using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class BreakStatementExecuter : StatementExecuter
    {
        public BreakStatementExecuter(BreakStatement stmt) : base(stmt)
        {
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            return ExecutionResult.Break;
        }
    }

}
