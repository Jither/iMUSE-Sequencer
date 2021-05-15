using ImuseSequencer.Verbs;
using Jither.CommandLine;
using Jither.Logging;
using System;

namespace ImuseSequencer
{
    class Program
    {
        private static readonly Logger logger = LogProvider.Get(nameof(Program));

        static void Main(string[] args)
        {
            LogProvider.RegisterLog(
                new StyledConsoleLog("{message}")
                .WithStyle("b", null, ConsoleStyleFlags.Bold)
                .WithStyle("gray", "a0a0a0")
                .WithStyle("dark", "606060")
                .WithStyle("yellow", "ffcc00")
                .WithStyle("green", "88cc55")
                .WithStyle("blue", "88bbff")
                .WithStyle("red", "dd6666")
                .WithStyle("darkblue", "6699cc")
                .WithStyle("purple", "ccaaff")
            );

            try
            {
                var parser = new CommandParser()
                    .WithVerb<PlayOptions>(o => new PlayCommand(Settings.Default, o).Execute())
                    .WithVerb<RunOptions>(o => new RunCommand(Settings.Default, o).Execute())
                    .WithVerb<DumpOptions>(o => new DumpCommand(Settings.Default, o).Execute())
                    .WithVerb<ListOutputsOptions>(o => new ListOutputsCommand(Settings.Default, o).Execute())
                    .WithVerb<ScanOptions>(o => new ScanCommand(Settings.Default, o).Execute())
                    .WithErrorHandler(err => err.Parser.WriteHelp(err));

                parser.Parse(args);
            }
            catch (ImuseSequencerException ex)
            {
                logger.Error(ex.Message);
            }
        }
    }
}
