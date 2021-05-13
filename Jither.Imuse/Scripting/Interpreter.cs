using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Runtime.Executers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting
{
    public class Interpreter
    {
        private readonly ScriptExecuter script;
        private ExecutionContext context;

        public Interpreter(Script script)
        {
            this.script = new ScriptExecuter(script);
        }

        public void Execute(ImuseEngine engine)
        {
            context = new ExecutionContext(engine);
            script.Execute(context);
            context.Dump();
        }
    }
}
