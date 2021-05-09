using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Runtime
{
    public class ScriptExecuter : Executer
    {
        private readonly List<DeclarationExecuter> declarations;

        public ScriptExecuter(Script script) : base()
        {
            this.declarations = new List<DeclarationExecuter>();
            foreach (var declaration in script.Declarations)
            {
                this.declarations.Add(DeclarationExecuter.Build(declaration));
            }
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
