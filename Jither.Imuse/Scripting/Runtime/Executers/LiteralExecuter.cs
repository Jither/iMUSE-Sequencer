using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class LiteralExecuter : ExpressionExecuter
    {
        private readonly RuntimeValue value;

        public LiteralExecuter(Literal literal) : base(literal)
        {
            value = literal.ValueType switch
            {
                LiteralType.Boolean => literal.BooleanValue ? BooleanValue.True : BooleanValue.False,
                LiteralType.Integer => IntegerValue.Create(literal.IntegerValue),
                LiteralType.String => new StringValue(literal.StringValue),
                LiteralType.Time => new TimeValue(literal.TimeValue),
                _ => throw new NotImplementedException($"Literal type {literal.ValueType} is not implemented in interpreter")
            };
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            var result = value;

            // Format string literal:
            if (result is StringValue str)
            {
                result = str.Format(this, context);
            }

            return result;
        }
    }
}
