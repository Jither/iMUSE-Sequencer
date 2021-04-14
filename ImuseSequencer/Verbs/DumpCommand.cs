﻿using ImuseSequencer.Helpers;
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
    [Verb("dump", Help = "Dumps information on MIDI file")]
    public class DumpOptions
    {
        [Positional(0, Help = "Input MIDI file (Standard MIDI file or LEC chunk - SOUN, SOU, ADL, ROL etc.)", Required = true)]
        public string InputPath { get; set; }

        [Option('n', "notes", Help = "Dump note-on/off MIDI events")]
        public bool IncludeNotes { get; set; }

        [Option('e', "events", Help = "Dump MIDI events (other than note-on/off)")]
        public bool IncludeEvents { get; set; }

        [Option('t', "timeline", Help = "Dump timeline")]
        public bool IncludeTimeline { get; set; }
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

            if (options.IncludeEvents || options.IncludeNotes)
            {
                logger.Info("");
                logger.Info("<b>Events</b>");
                foreach (var track in midiFile.Tracks)
                {
                    logger.Info("");
                    logger.Info(track.ToString());
                    foreach (var evt in track.Events)
                    {
                        if (evt.Message is NoteOnMessage or NoteOffMessage)
                        {
                            if (options.IncludeNotes)
                            {
                                logger.Colored($"  {evt}", GetColor(evt));
                            }
                        }
                        else
                        {
                            if (options.IncludeEvents)
                            {
                                logger.Colored($"  {evt}", GetColor(evt));
                            }
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
