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
        private readonly FileProvider fileProvider;

        public ExecutionContext Context { get; private set; }

        public Interpreter(string source, FileProvider fileProvider)
        {
            this.fileProvider = fileProvider;
            var parser = new ScriptParser(source);
            var ast = parser.Parse();
            this.script = new ScriptExecuter(ast);
        }

        public void Execute(ImuseEngine engine)
        {
            Context = new ExecutionContext(engine, engine.Commands, engine.Events, engine.Queue, fileProvider);
            script.Execute(Context);
            engine.Events.TriggerStart(Context);
        }
    }
}
