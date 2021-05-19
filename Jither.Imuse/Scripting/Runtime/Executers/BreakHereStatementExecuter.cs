using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class BreakHereStatementExecuter : StatementExecuter
    {
        public ExpressionExecuter Count { get; }

        public BreakHereStatementExecuter(BreakHereStatement stmt) : base(stmt)
        {
            if (stmt.Count != null)
            {
                Count = ExpressionExecuter.Build(stmt.Count);
            }
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            // Outside handling
            throw new System.NotImplementedException();
        }
    }
}
