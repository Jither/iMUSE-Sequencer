using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class DefineDeclaration : Declaration
    {
        public override NodeType Type => NodeType.DefineDeclaration;
        public Identifier Identifier { get; }
        // TODO: Consider expression for define value
        public Literal Value { get; }

        public DefineDeclaration(Identifier identifier, Literal value)
        {
            Identifier = identifier;
            Value = value;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Identifier;
                yield return Value;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitDefineDeclaration(this);
    }
}
