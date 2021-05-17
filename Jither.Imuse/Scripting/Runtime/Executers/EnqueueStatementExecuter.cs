using Jither.Imuse.Commands;
using Jither.Imuse.Scripting.Ast;
using System.Collections.Generic;

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
            if (context.EnqueuingCommands != null)
            {
                ErrorHelper.ThrowInvalidOperationError(Node, "Cannot enqueue while another queue is being built (no nested enqueue statements)");
            }
            
            var soundIdValue = soundId.GetValue(context).AsInteger(soundId);
            var markerIdValue = markerId.GetValue(context).AsInteger(markerId);
            var commandList = new List<CommandCall>();
            
            // Start enqueueing state
            context.EnqueuingCommands = commandList;
            
            body.Execute(context);
            context.Queue.Enqueue(soundIdValue, markerIdValue, commandList);

            // End of enqueuing state
            context.EnqueuingCommands = null;

            return ExecutionResult.Void;
        }
    }
}
