using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class EnqueueStatement : Statement
    {
        public override NodeType Type => NodeType.EnqueueStatement;
        public Expression SoundId { get; }
        public Expression MarkerId { get; }
        public List<Statement> Body { get; }

        public EnqueueStatement(Expression soundId, Expression markerId, List<Statement> body)
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
                foreach (var stmt in Body)
                {
                    yield return stmt;
                }
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitEnqueueStatement(this);
    }
}
