using Jither.CommandLine;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    public class CommonOptions
    {
        [Option('l', "log-level", ArgName = "level", Default = LogLevel.Info, Help = "Specifies verbosity of output")]
        public LogLevel LogLevel { get; set; }
    }
}
