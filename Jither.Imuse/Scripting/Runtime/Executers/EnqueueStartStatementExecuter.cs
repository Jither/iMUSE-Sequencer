using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class EnqueueStartStatementExecuter : StatementExecuter
    {
        private readonly ExpressionExecuter soundId;
        private readonly ExpressionExecuter markerId;

        public EnqueueStartStatementExecuter(EnqueueStartStatement stmt) : base(stmt)
        {
            soundId = ExpressionExecuter.Build(stmt.SoundId);
            markerId = ExpressionExecuter.Build(stmt.MarkerId);
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            if (context.EnqueuingCommands != null)
            {
                ErrorHelper.ThrowInvalidOperationError(Node, "Cannot enqueue while another queue is being built (no nested enqueue statements)");
            }

            var soundIdValue = soundId.Execute(context).AsInteger(soundId);
            var markerIdValue = markerId.Execute(context).AsInteger(markerId);
            var commandList = new EnqueueCommandList(soundIdValue, markerIdValue);

            // Start enqueueing state
            context.EnqueuingCommands = commandList;

            return RuntimeValue.Void;
        }
    }
}
