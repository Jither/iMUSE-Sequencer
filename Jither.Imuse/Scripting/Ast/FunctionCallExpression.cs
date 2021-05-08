using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class FunctionCallExpression : Expression
    {
        public override NodeType Type => NodeType.FunctionCallExpression;
        public Identifier Name { get; }
        public List<Expression> Arguments { get; }

        public FunctionCallExpression(Identifier name, List<Expression> arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Name;
                foreach (var arg in Arguments)
                {
                    yield return arg;
                }
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitFunctionCallExpression(this);
    }
}
