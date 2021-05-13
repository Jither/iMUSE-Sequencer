using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class EnqueueStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter soundId;
        private readonly ExpressionExecuter markerId;
        private readonly StatementExecuter body;

        public EnqueueStatementExecuter(EnqueueStatement stmt) : base(stmt)
        {
            soundId = ExpressionExecuter.Build(stmt.SoundId);
            markerId = ExpressionExecuter.Build(stmt.MarkerId);
            body = Build(stmt.Body);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
