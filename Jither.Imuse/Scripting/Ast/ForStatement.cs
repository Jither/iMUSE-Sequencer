﻿using Jither.Imuse.Scripting;
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
        public Statement Body { get; }

        public ForStatement(Identifier iterator, Expression from, Expression to, bool increment, Statement body)
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
                yield return Body;
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitForStatement(this);
    }
}
