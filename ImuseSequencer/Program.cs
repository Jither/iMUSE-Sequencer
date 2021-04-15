using ImuseSequencer.Verbs;
using Jither.CommandLine;
using Jither.Logging;
using System;

namespace ImuseSequencer
{
    class Program
    {
        static void Main(string[] args)
        {
            LogProvider.RegisterLog(new ConsoleLog("{message}"));

            var parser = new CommandParser()
                .WithVerb<DumpOptions>(o => new DumpCommand(o).Execute())
                .WithVerb<ListOutputsOptions>(o => new ListOutputsCommand(o).Execute())
                .WithVerb<TestOptions>(o => new TestCommand(o).Execute())
                .WithErrorHandler(err => err.Parser.WriteHelp(err));

            parser.Parse(args);
        }
    }
}
