using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class LiteralExecuter : ExpressionExecuter
    {
        private readonly Literal literal;
        private readonly RuntimeValue value;

        public LiteralExecuter(Literal literal) : base(literal)
        {
            this.literal = literal;
            value = literal.ValueType switch
            {
                LiteralType.Boolean => literal.BooleanValue ? BooleanValue.True : BooleanValue.False,
                LiteralType.Integer => IntegerValue.Create(literal.IntegerValue),
                LiteralType.String => new StringValue(literal.StringValue),
                _ => throw new NotImplementedException($"Literal type {literal.ValueType} is not implemented in interpreter")
            };
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            // TODO: ExecutionResult here could actually be pre-calculated too
            return new ExecutionResult(ExecutionResultType.Normal, value);
        }
    }
}
