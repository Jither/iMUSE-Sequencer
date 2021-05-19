using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class ActionDeclaration : Declaration
    {
        public override NodeType Type => NodeType.ActionDeclaration;
        public Expression During { get; }
        public Identifier Name { get; }
        public BlockStatement Body { get; private set;  }

        public ActionDeclaration(Identifier name, Expression during, BlockStatement body)
        {
            Name = name;
            During = during;
            Body = body;
        }

        internal void ReplaceBody(BlockStatement body)
        {
            Body = body;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                if (Name != null)
                {
                    yield return Name;
                }
                if (During != null)
                {
                    yield return During;
                }
                yield return Body;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitActionDeclaration(this);
    }
}
