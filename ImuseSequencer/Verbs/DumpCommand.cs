using ImuseSequencer.Helpers;
using Jither.CommandLine;
using Jither.Logging;
using Jither.Midi.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    [Verb("dump")]
    public class DumpOptions
    {
        [Positional(0, Help = "Input MIDI file (Standard MIDI file or LEC chunk - SOUN, SOU, ADL, ROL etc.)", Required = true)]
        public string InputPath { get; set; }
    }

    public class DumpCommand
    {
        private Logger logger = LogProvider.Get(nameof(DumpCommand));

        private DumpOptions options;

        public DumpCommand(DumpOptions options)
        {
            this.options = options;
        }

        public void Execute()
        {
            var midiFile = new MidiFile(options.InputPath);
            logger.Info(midiFile.ToString());

            foreach (var track in midiFile.Tracks)
            {
                logger.Info("");
                logger.Info(track.ToString());
                foreach (var evt in track.Events)
                {
                    logger.Colored($"  {evt}", GetColor(evt));
                }
            }
        }

        private string GetColor(MidiEvent evt)
        {
            return evt.Message switch
            {
                NoteOnMessage => "a0a0a0",
                NoteOffMessage => "606060",
                ControlChangeMessage => "ffcc00",
                ProgramChangeMessage => "88cc55",
                SysexMessage => "dd6666",
                PitchBendChangeMessage => "6699cc",
                MetaMessage => "ccaaff",
                _ => null
            };
        }
    }
}
