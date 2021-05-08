using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class SoundsDeclaration : Declaration
    {
        public override NodeType Type => NodeType.SoundsDeclaration;
        public List<SoundDeclarator> Sounds { get; }

        public SoundsDeclaration(List<SoundDeclarator> sounds)
        {
            Sounds = sounds;
        }

        public override IEnumerable<Node> Children => Sounds;
        public override void Accept(IAstVisitor visitor) => visitor.VisitSoundsDeclaration(this);
    }
}
