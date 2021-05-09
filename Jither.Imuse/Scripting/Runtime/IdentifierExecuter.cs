using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public class IdentifierExecuter : ExpressionExecuter
    {
        private readonly Identifier identifier;

        public IdentifierExecuter(Identifier identifier)
        {
            this.identifier = identifier;
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
