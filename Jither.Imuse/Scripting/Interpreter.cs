using Jither.Imuse.Commands;
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
        private readonly MuskCommands scriptCommands = new();

        public ExecutionContext Context { get; private set; }

        public Interpreter(string source, FileProvider fileProvider)
        {
            this.fileProvider = fileProvider;
            var parser = new ScriptParser(source);
            var ast = parser.Parse();
            var flattener = new ActionFlattener();
            flattener.Execute(ast);
            this.script = new ScriptExecuter(ast);
        }

        public void Execute(ImuseEngine engine)
        {
            Context = new ExecutionContext(engine, engine.Events, engine.Queue, fileProvider);
            
            // iMUSE specific commands:
            Context.AddCommands(engine.Commands);
            // Scripting commands (e.g. random, print-line...)
            Context.AddCommands(scriptCommands);

            Context.EnterScope("Global");

            script.Execute(Context);
            engine.Events.TriggerStart(Context);
        }
    }
}
