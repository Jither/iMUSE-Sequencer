using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class DoStatementExecuter : StatementExecuter
    {
        private readonly StatementExecuter body;
        private readonly ExpressionExecuter test;

        public DoStatementExecuter(DoStatement doStmt) : base(doStmt)
        {
            body = Build(doStmt.Body);
            test = ExpressionExecuter.Build(doStmt.Test);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            while (true)
            {
                var result = body.Execute(context);
                if (result.Type == ExecutionResultType.Break)
                {
                    break;
                }
                // do-until, so we exit when the test is TRUE
                if (test.GetValue(context).AsBoolean(this))
                {
                    break;
                }
            }
            return ExecutionResult.Void;
        }
    }

}
