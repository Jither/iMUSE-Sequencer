using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class IdentifierExecuter : ExpressionExecuter
    {
        private readonly Identifier identifier;

        public string Name => identifier.Name;

        public IdentifierExecuter(Identifier identifier) : base(identifier)
        {
            this.identifier = identifier;
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            var symbol = context.CurrentScope.GetSymbol(identifier, identifier.Name);
            return symbol.Value;
        }
    }
}
