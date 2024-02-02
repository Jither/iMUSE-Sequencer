using Jither.CommandLine;
using Jither.Logging;
using Misty.Verbs;

namespace Misty;

internal class Program
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
                .WithVerb<RemapOptions>(o => new RemapCommand(o).Execute())
                .WithVerb<MapInfoOptions>(o => new MapInfoCommand(o).Execute())
                .WithErrorHandler(err => err.Parser.WriteHelp(err));

            parser.Parse(args);
        }
        catch (MistyException ex)
        {
            logger.Error(ex.Message);
        }
    }
}
