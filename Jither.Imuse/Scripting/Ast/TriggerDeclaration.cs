using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class TriggerDeclaration : Declaration
    {
        public override NodeType Type => NodeType.TriggerDeclaration;
        public Expression During { get; }
        public Identifier Id { get; }
        public Statement Body { get; }

        public TriggerDeclaration(Identifier id, Expression during, Statement body)
        {
            Id = id;
            During = during;
            Body = body;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                if (Id != null)
                {
                    yield return Id;
                }
                if (During != null)
                {
                    yield return During;
                }
                yield return Body;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitTriggerDeclaration(this);
    }
}
