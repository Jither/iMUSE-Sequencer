using Jither.Imuse.Scripting;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting.Ast
{
    public class Identifier : Expression
    {
        public override NodeType Type => NodeType.Identifier;
        public string Name { get; }

        public Identifier(string name)
        {
            Name = name;
        }

        public override IEnumerable<Node> Children => Enumerable.Empty<Node>();
        public override void Accept(IAstVisitor visitor) => visitor.VisitIdentifier(this);
    }
}
