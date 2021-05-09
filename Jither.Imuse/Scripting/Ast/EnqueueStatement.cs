using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class EnqueueStatement : Statement
    {
        public override NodeType Type => NodeType.EnqueueStatement;
        public Expression SoundId { get; }
        public Expression MarkerId { get; }
        public Statement Body { get; }

        public EnqueueStatement(Expression soundId, Expression markerId, Statement body)
        {
            SoundId = soundId;
            MarkerId = markerId;
            Body = body;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return SoundId;
                yield return MarkerId;
                yield return Body;
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitEnqueueStatement(this);
    }
}
