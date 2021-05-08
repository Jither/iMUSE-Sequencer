using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class Script : Node
    {
        public override NodeType Type => NodeType.Script;
        public List<Declaration> Declarations { get; }

        public Script(List<Declaration> declarations)
        {
            Declarations = declarations;
        }

        public override IEnumerable<Node> Children => Declarations;
        public override void Accept(IAstVisitor visitor) => visitor.VisitScript(this);
    }
}
