using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class WhileStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter test;
        private readonly StatementExecuter body;

        public WhileStatementExecuter(WhileStatement stmt) : base(stmt)
        {
            test = ExpressionExecuter.Build(stmt.Test);
            body = Build(stmt.Body);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            while (test.GetValue(context).AsBoolean(test))
            {
                var result = body.Execute(context);
                if (result.Type == ExecutionResultType.Break)
                {
                    break;
                }
            }
            return ExecutionResult.Void;
        }
    }

}
