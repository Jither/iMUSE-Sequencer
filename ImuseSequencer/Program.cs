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
            LogProvider.RegisterLog(new ConsoleLog("{message}"));

            try
            {
                var parser = new CommandParser()
                    .WithVerb<PlayOptions>(o => new PlayCommand(o).Execute())
                    .WithVerb<RunOptions>(o => new RunCommand(o).Execute())
                    .WithVerb<DumpOptions>(o => new DumpCommand(o).Execute())
                    .WithVerb<ListOutputsOptions>(o => new ListOutputsCommand(o).Execute())
                    .WithVerb<ScanOptions>(o => new ScanCommand(o).Execute())
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
