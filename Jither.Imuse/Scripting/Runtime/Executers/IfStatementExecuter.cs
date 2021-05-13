using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class IfStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter test;
        private readonly StatementExecuter consequent;
        private readonly StatementExecuter alternate;

        public IfStatementExecuter(IfStatement stmt) : base(stmt)
        {
            test = ExpressionExecuter.Build(stmt.Test);
            consequent = Build(stmt.Consequent);
            if (stmt.Alternate != null)
            {
                alternate = Build(stmt.Alternate);
            }
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            if (test.GetValue(context).AsBoolean(test))
            {
                return consequent.Execute(context);
            }
            
            if (alternate != null)
            {
                return alternate.Execute(context);
            }

            return ExecutionResult.Void;
        }
    }
}
