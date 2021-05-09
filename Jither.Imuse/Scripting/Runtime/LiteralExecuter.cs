using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class LiteralExecuter : ExpressionExecuter
    {
        private readonly Literal literal;

        public LiteralExecuter()
        {
        }

        public LiteralExecuter(Literal literal)
        {
            this.literal = literal;
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
