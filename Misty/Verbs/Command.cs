using Jither.CommandLine;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Misty.Verbs;

public class CommonOptions
{
    [Option('l', "log-level", ArgName = "level", Default = LogLevel.Info, Help = "Specifies verbosity of output")]
    public LogLevel LogLevel { get; set; }
}

public abstract class Command<TOptions> where TOptions : CommonOptions
{
    protected readonly Logger logger = LogProvider.Get(nameof(Command<TOptions>));

    protected readonly TOptions options;

    protected Command(TOptions options)
    {
        this.options = options;
        LogProvider.Level = options.LogLevel;
    }

    public abstract void Execute();
}