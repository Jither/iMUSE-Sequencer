using ImuseSequencer.Helpers;
using Jither.CommandLine;
using Jither.Logging;
using Jither.Midi.Messages;
using Jither.Midi.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    [Verb("dump", Help = "Dumps information on MIDI file")]
    public class DumpOptions
    {
        [Positional(0, Name = "input path", Help = "Input MIDI file (Standard MIDI file or LEC chunk - SOUN, SOU, ADL, ROL etc.)", Required = true)]
        public string InputPath { get; set; }

        [Option('n', "notes", Help = "Dump note-on/off MIDI events")]
        public bool IncludeNotes { get; set; }

        [Option('e', "events", Help = "Dump MIDI events (other than note-on/off)")]
        public bool IncludeEvents { get; set; }

        [Option('i', "imuse", Help = "Dump iMUSE MIDI events")]
        public bool IncludeImuse { get; set; }

        [Option('t', "timeline", Help = "Dump timeline")]
        public bool IncludeTimeline { get; set; }

        [Examples]
        public static IEnumerable<Example<DumpOptions>> Examples => new[]
{
            new Example<DumpOptions>("Dump events excluding notes", new DumpOptions { InputPath = "LARGO.rol", IncludeEvents = true }),
            new Example<DumpOptions>("Dump events including notes", new DumpOptions { InputPath = "OFFICE.mid", IncludeEvents = true, IncludeNotes = true }),
            new Example<DumpOptions>("Dump only iMUSE events", new DumpOptions { InputPath = "CRABROOM.adl", IncludeImuse = true })
        };
    }

    public class DumpCommand
    {
        private readonly Logger logger = LogProvider.Get(nameof(DumpCommand));

        private readonly DumpOptions options;

        public DumpCommand(DumpOptions options)
        {
            this.options = options;
        }

        public void Execute()
        {
            MidiFile midiFile;
            try
            {
                midiFile = new MidiFile(options.InputPath, new MidiFileOptions { ParseImuse = true });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                throw new ImuseSequencerException($"Cannot open file: {ex.Message}", ex);
            }

            logger.Info($"MIDI: {midiFile}");
            
            if (midiFile.ImuseHeader != null)
            {
                logger.Info($"iMUSE header (MDhd): {midiFile.ImuseHeader}");
            }
            else
            {
                logger.Info($"iMUSE header (MDhd): NONE");
            }

            if (options.IncludeEvents || options.IncludeNotes || options.IncludeImuse)
            {
                logger.Info("");
                logger.Info("<b>Events</b>");
                foreach (var track in midiFile.Tracks)
                {
                    logger.Info("");
                    logger.Info(track.ToString());
                    foreach (var evt in track.Events)
                    {
                        bool output = evt.Message switch
                        {
                            NoteOnMessage or NoteOffMessage => options.IncludeNotes,
                            ImuseMessage => options.IncludeImuse || options.IncludeEvents,
                            _ => options.IncludeEvents
                        };
                        if (output)
                        {
                            logger.Colored($"  {evt}", GetColor(evt));
                        }
                    }
                }
            }

            if (options.IncludeTimeline)
            {
                logger.Info("");
                logger.Info("<b>Timeline</b>");
                foreach (var time in midiFile.Timeline)
                {
                    logger.Info(time.ToString());
                }
            }
        }

        private static string GetColor(MidiEvent evt)
        {
            return evt.Message switch
            {
                NoteOnMessage => "a0a0a0",
                NoteOffMessage => "606060",
                ControlChangeMessage => "ffcc00",
                ProgramChangeMessage => "88cc55",
                ImuseMessage => "88bbff",
                SysexMessage => "dd6666",
                PitchBendChangeMessage => "6699cc",
                MetaMessage => "ccaaff",
                _ => null
            };
        }
    }
}
