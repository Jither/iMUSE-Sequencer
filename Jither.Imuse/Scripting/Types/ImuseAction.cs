using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Runtime.Executers;
using System;

namespace Jither.Imuse.Scripting.Types
{
    public class ImuseAction
    {
        public string Name { get; }
        public int? During { get; }
        private readonly BlockStatementExecuter bodyExecuter;

        public ImuseAction(string name, int? during, BlockStatementExecuter bodyExecuter)
        {
            Name = name;
            During = during;
            this.bodyExecuter = bodyExecuter;
        }

        internal void Execute(ExecutionContext context)
        {
            context.EnterScope($"Action {Name}");
            bodyExecuter.Execute(context);
            context.ExitScope();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
