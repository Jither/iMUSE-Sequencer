using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class CaseStatement : Statement
    {
        public override NodeType Type => NodeType.CaseStatement;
        public Expression Discriminant { get; }
        public List<CaseDefinition> Cases { get; }

        public CaseStatement(Expression test, List<CaseDefinition> cases)
        {
            Discriminant = test;
            Cases = cases;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Discriminant;
                foreach (var def in Cases)
                {
                    yield return def;
                }
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitCaseStatement(this);
    }

    public class CaseDefinition : Node
    {
        public override NodeType Type => NodeType.CaseDefinition;
        public Literal Test { get; }
        public Statement Consequent { get; }

        public CaseDefinition(Literal test, Statement consequent)
        {
            Test = test;
            Consequent = consequent;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Test;
                yield return Consequent;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitCaseDefinition(this);
    }
}
