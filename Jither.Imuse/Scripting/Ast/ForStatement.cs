using Jither.Imuse.Scripting;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Ast
{
    public class ForStatement : Statement
    {
        public override NodeType Type => NodeType.ForStatement;
        public Identifier Iterator { get; }
        public Expression From { get; }
        public Expression To { get; }
        public bool Increment { get; }
        public List<Statement> Body { get; }

        public ForStatement(Identifier iterator, Expression from, Expression to, bool increment, List<Statement> body)
        {
            Iterator = iterator;
            From = from;
            To = to;
            Increment = increment;
            Body = body;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Iterator;
                yield return From;
                yield return To;
                foreach (var stmt in Body)
                {
                    yield return stmt;
                }
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitForStatement(this);
    }
}
