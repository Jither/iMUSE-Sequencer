using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime
{
    public class ExecutionContext
    {
        public ImuseEngine Engine { get; }
        public Scope CurrentScope => scopes.Peek();

        private readonly Stack<Scope> scopes = new();

        public ExecutionContext(ImuseEngine engine)
        {
            Engine = engine;
            scopes.Push(new Scope(null));
        }
    }
}
