using Jither.CommandLine;
using Jither.Imuse;
using Jither.Imuse.Scripting;
using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Runtime;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    [Verb("run", Help = "Runs an iMUSE script")]
    public class RunOptions : CommonOptions
    {
        [Positional(0, Name ="script path", Help = "Path to iMUSE script", Required = true)]
        public string ScriptPath { get; set; }
    }

    public class RunCommand : Command
    {
        private static readonly Logger logger = LogProvider.Get(nameof(RunCommand));
        private readonly RunOptions options;

        public RunCommand(RunOptions options) : base(options)
        {
            this.options = options;
        }

        public override void Execute()
        {
            var source = File.ReadAllText(options.ScriptPath, Encoding.UTF8);
            try
            {
                var parser = new ScriptParser(source);
                var ast = parser.Parse();
                var interpreter = new Interpreter(ast);
                var engine = new ImuseEngine(new NullTransmitter(), SoundTarget.Roland);
                interpreter.Execute(engine);
            }
            catch (Exception ex) when (ex is ScriptException exScript)
            {
                logger.Error(ex.Message);
                OutputSourceWithLocation(source, exScript.Range);
            }
        }

        private void OutputSourceWithLocation(string source, SourceRange range)
        {
            var builder = new StringBuilder();
            builder.Append(source[..range.Start.Index]);
            builder.Append("<c#ee5000>");
            builder.Append(source[range.Start.Index..range.End.Index]);
            builder.Append("</c>");
            builder.Append(source[range.End.Index..]);
            logger.Info(builder.ToString());
        }
    }
}
