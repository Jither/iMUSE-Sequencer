using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class EnqueueStartStatement : Statement
    {
        public Expression SoundId { get; }
        public Expression MarkerId { get; }

        public override NodeType Type => NodeType.EnqueueStartStatement;

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return SoundId;
                yield return MarkerId;
            }
        }

        public EnqueueStartStatement(EnqueueStatement enqueue)
        {
            SoundId = enqueue.SoundId;
            MarkerId = enqueue.MarkerId;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitEnqueueStartStatement(this);
        }
    }
}
