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
        private long ticks = 0;

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

        public void Tick()
        {
            ticks++;
            // TODO: Temporary hardcode: 32 ticks = 1 frame at 30fps (assuming 120 bpm => 120 * 480 ticks per minute / 60 = 960 ticks per second = 960 / 30 = 32 ticks per frame)
            if ((ticks % 32) == 0)
            {
                Context.ResumeScripts();
            }
        }
    }
}
