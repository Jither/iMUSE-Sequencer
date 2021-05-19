using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class ConditionalJumpStatement : JumpStatement
    {
        public Expression Test { get; }
        public bool JumpWhen { get; }

        public override NodeType Type => NodeType.ConditionalJumpStatement;

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Test;
            }
        }

        public ConditionalJumpStatement(Label destination, Expression test, bool jumpWhen) : base(destination)
        {
            Test = test;
            JumpWhen = jumpWhen;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitConditionalJumpStatement(this);
        }
    }
}
