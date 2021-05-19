using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class ScriptExecuter : Executer
    {
        private readonly List<DeclarationExecuter> declarations;

        public ScriptExecuter(Script script) : base(script)
        {
            this.declarations = new List<DeclarationExecuter>();
            foreach (var declaration in script.Declarations)
            {
                this.declarations.Add(DeclarationExecuter.Build(declaration));
            }
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            foreach (var declaration in declarations)
            {
                declaration.Execute(context);
            }
            return RuntimeValue.Void;
        }
    }
}
