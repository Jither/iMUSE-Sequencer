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
            // Quick hack - we actually enqueue the entire body as a "closure" of sorts
            // Original iMUSE enqueues specific queueable commands explicitly.
            context.Queue.Enqueue(soundId.GetValue(context).AsInteger(soundId), markerId.GetValue(context).AsInteger(markerId), body, context);
            return ExecutionResult.Void;
        }
    }
}
