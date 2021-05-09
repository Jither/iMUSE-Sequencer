using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Runtime;
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

        public Interpreter(Script script)
        {
            this.script = new ScriptExecuter(script);
        }

        public void Execute()
        {
        }
    }
}
