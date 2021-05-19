using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class EnqueueEndStatementExecuter : StatementExecuter
    {
        public EnqueueEndStatementExecuter(Statement stmt) : base(stmt)
        {
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            var item = context.EnqueuingCommands;
            if (item == null)
            {
                ErrorHelper.ThrowInvalidOperationError(Node, "End of enqueue, but no enqueue was started");
            }

            context.Queue.Enqueue(item.SoundId, item.MarkerId, item);

            // End of enqueuing state
            context.EnqueuingCommands = null;

            return RuntimeValue.Void;
        }
    }
}
