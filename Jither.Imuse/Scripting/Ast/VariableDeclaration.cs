using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class VariableDeclaration : Declaration
    {
        public override NodeType Type => NodeType.VariableDeclaration;
        public Identifier Identifier { get; }
        public Expression Value { get; }

        public VariableDeclaration(Identifier identifier, Expression value)
        {
            Identifier = identifier;
            Value = value;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Identifier;
                if (Value != null)
                {
                    yield return Value;
                }
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitVariableDeclaration(this);
    }
}
