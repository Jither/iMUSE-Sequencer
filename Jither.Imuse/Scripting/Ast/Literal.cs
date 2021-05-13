using System.Collections.Generic;
using System.Linq;
using Jither.Imuse.Scripting;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Ast
{
    public class Literal : Expression
    {
        public override NodeType Type => NodeType.Literal;
        public LiteralType ValueType { get; }

        public string StringValue { get; }
        public bool BooleanValue { get; }
        public Time TimeValue { get; }
        public int IntegerValue { get; }

        public object Value { get; }

        public Literal(LiteralType type, string value)
        {
            ValueType = type;
            Value = StringValue = value;
        }

        public Literal(LiteralType type, bool value)
        {
            ValueType = type;
            Value = BooleanValue = value;
        }

        public Literal(LiteralType type, Time value)
        {
            ValueType = type;
            Value = TimeValue = value;
        }

        public Literal(LiteralType type, int value)
        {
            ValueType = type;
            Value = IntegerValue = value;
        }

        public override IEnumerable<Node> Children => Enumerable.Empty<Node>();
        public override void Accept(IAstVisitor visitor) => visitor.VisitLiteral(this);
    }
}
