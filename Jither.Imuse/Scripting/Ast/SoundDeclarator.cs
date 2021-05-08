using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class SoundDeclarator : Node
    {
        public override NodeType Type => NodeType.SoundDeclarator;
        public Literal Name { get; }
        public Expression Id { get; }

        public SoundDeclarator(Literal name, Expression id)
        {
            Name = name;
            Id = id;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Name;
                yield return Id;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitSoundDeclarator(this);
    }
}
