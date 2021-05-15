using Jither.CommandLine;
using Jither.Imuse;
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

    public class CommonPlaybackOptions : CommonOptions, ICustomParsing
    {
        [Option('m', "max-slots", Help = "Sets number of slots to maximum (15) rather than original iMUSE's 9 for General Midi and 8 for Roland.")]
        public bool MaxSlots { get; set; }

        [Option('c', "clean-jumps", Help = "Cuts off otherwise infinitely sustained loops when performing jumps.")]
        public bool CleanJumps { get; set; }

        [Option("loop-limit", Help = "Limits the number of loops performed by the sequencer. This is useful (necessary) for e.g. non-interactive recording. 0 indicates no limit for device outputs and 3 for MIDI file output.", ArgName = "limit", Default = 0)]
        public int LoopLimit { get; set; }

        [Option("jump-limit", Help = "Limits the number of times each jump hook is performed by the sequencer. This is useful (necessary) for e.g. non-interactive recording. 0 indicates no limit for device outputs and 3 for MIDI file output.", ArgName = "limit", Default = 0)]
        public int JumpLimit { get; set; }

        [Option("latency", Help = "Latency (in ticks) of playback. Only has an effect on device output. You probably shouldn't set this to something silly like 1...", ArgName = "ticks", Default = 480)]
        public int Latency { get; set; }

        public ImuseOptions ImuseOptions => new() { JumpLimit = JumpLimit, LoopLimit = LoopLimit, MaxSlots = MaxSlots, CleanJumps = CleanJumps
    };


    public virtual void AfterParse()
        {
            if (JumpLimit == 0)
            {
                JumpLimit = int.MaxValue;
            }

            if (LoopLimit == 0)
            {
                LoopLimit = int.MaxValue;
            }

            if (Latency < 1)
            {
                throw new CustomParserException("Don't set latency to a silly value...");
            }
        }
    }
}
