using Jither.Logging;
using Jither.Midi.Messages;
using Jither.Midi.Files;
using System.Collections.Generic;
using System.Linq;
using Jither.Midi.Helpers;
using System;

namespace Jither.Imuse
{
    /// <summary>
    /// The sustain module is used during jumps (and loops). It finds all notes that should be sustained across
    /// the jump, and schedules note-offs for those notes at the correct point at the location after the jump.
    /// It also ensures that those note-offs won't interfere if the same note is played at the new location before
    /// the sustain ends.
    /// </summary>
    /// <remarks>
    /// Note that the sustain module is shared among all the sequencers in the engine.
    /// </remarks>
    // TODO: Unlike iMUSE, which only allows 24 sustained notes at a time, this implementation is unlimited.
    public class Sustainer
    {
        private class SustainDefinition
        {
            public int Note { get; }
            public int Channel { get; }
            public Sequencer Sequencer { get; }
            public long SustainTicks { get; }
            public int TickCount { get; set; }

            public SustainDefinition(Sequencer sequencer, int note, int channel, long sustainTime)
            {
                Sequencer = sequencer;
                Note = note;
                Channel = channel;
                SustainTicks = sustainTime;
                TickCount = 0;
            }

            public override string ToString()
            {
                return $"Sustain: {MidiHelper.NoteNumberToName(Note)} ({Note}) on seq {Sequencer.Index}, channel {Channel} will be sustained for {SustainTicks} ticks";
            }
        }

        private enum SeekNoteStatus
        {
            NoteOnFound,
            NoteOffFound,
            NoteNotFound,
            ReachedEndOfTrack
        }

        private class SeekNoteResult
        {
            public static readonly SeekNoteResult ReachedEndOfTrack = new(SeekNoteStatus.ReachedEndOfTrack, 0, 0, 0);
            public static readonly SeekNoteResult NoteNotFound = new(SeekNoteStatus.NoteNotFound, 0, 0, 0);

            public SeekNoteStatus Status { get; }
            public int Channel { get; }
            public int Note { get; }
            public int Velocity { get; }

            public SeekNoteResult(SeekNoteStatus status, int channel, int note, int velocity)
            {
                Status = status;
                Channel = channel;
                Note = note;
                Velocity = velocity;
            }
        }

        private static readonly Logger logger = LogProvider.Get(nameof(Sustainer));

        private readonly List<SustainDefinition> activeSustainDefs = new();
        private readonly HashSet<SustainedNote> noteTable = new();

        public Sustainer()
        {
        }

        /// <summary>
        /// Renders any pending note-offs for the next tick.
        /// </summary>
        /// <returns>
        /// <c>true</c> if more note-offs are still pending. Otherwise <c>false</c>.
        /// </returns>
        public bool Tick()
        {
            // Find sustain definitions at the current position and apply their note-offs.
            for (int i = activeSustainDefs.Count - 1; i >= 0; i--)
            {
                var sustainDef = activeSustainDefs[i];
                sustainDef.TickCount++;
                if (sustainDef.TickCount >= sustainDef.SustainTicks)
                {
                    // Velocity for note-offs doesn't matter. Driver will replace it.
                    var message = new NoteOffMessage(sustainDef.Channel, (byte)sustainDef.Note, 0);
                    logger.Verbose($"Stopping sustained note {message}");
                    sustainDef.Sequencer.Parts.HandleEvent(message);
                    activeSustainDefs.RemoveAt(i);
                }
            }
            return activeSustainDefs.Count > 0;
        }

        /// <summary>
        /// Analyzes a jump, preparing the sustain definitions that will be active after that jump.
        /// </summary>
        public void AnalyzeSustain(Sequencer sequencer, SequencerPointer oldTrackPos, SequencerPointer newTrackPos, long newSustainTicks)
        {
            var sustainDefs = new List<SustainDefinition>();

            // Get all notes that are currently on:
            noteTable.Clear();
            sequencer.Parts.GetSustainNotes(noteTable);

            // Now search forward from the old position for all note-offs for those notes:
            long sustainTicks = sequencer.NextEventTick - sequencer.CurrentTick;

            if (sustainTicks < 0)
            {
                sustainTicks = 0;
            }

            while (noteTable.Count > 0)
            {
                var result = SeekNote(oldTrackPos);

                if (result.Status == SeekNoteStatus.ReachedEndOfTrack)
                {
                    // We may reach end of track without finding note-off for all notes in noteTable.
                    // That would cause an infinite loop.
                    // (see e.g. STAN.rol or LECHUCK.rol). So abandon all hope:
                    break;
                }
                
                if (result.Status == SeekNoteStatus.NoteOffFound)
                {
                    var note = new SustainedNote(result.Channel, result.Note);
                    if (noteTable.Contains(note))
                    {
                        noteTable.Remove(note);
                        sustainDefs.Add(new SustainDefinition(sequencer, result.Note, result.Channel, sustainTicks));
                    }
                }
                sustainTicks = oldTrackPos.NextEventTick - sequencer.CurrentTick;
            }

            // Find the longest sustain we found. That's how far we need to search below.
            long maxTicks = 0;
            if (sustainDefs.Count > 0)
            {
                maxTicks = sustainDefs.Max(s => s.SustainTicks);
            }

            // Now search from the new position, and ensure old note-offs won't stump on new note-ons
            // (i.e., cut them off by way of a note-off ending up after the note-on of a different note).
            sustainTicks = newSustainTicks;
            while (sustainTicks < maxTicks)
            {
                var result = SeekNote(newTrackPos);
                if (result.Status == SeekNoteStatus.NoteOnFound)
                {
                    var sustainDef = sustainDefs.Find(s => s.Note == result.Note && s.Channel == result.Channel);
                    if (sustainDef != null && sustainTicks < sustainDef.SustainTicks)
                    {
                        sustainDefs.Remove(sustainDef);
                    }
                }
                sustainTicks = newTrackPos.NextEventTick - newSustainTicks;
            }

            activeSustainDefs.AddRange(sustainDefs);

            logger.Verbose(String.Join(Environment.NewLine, activeSustainDefs));
        }

        private SeekNoteResult SeekNote(SequencerPointer pos)
        {
            var message = pos.Event?.Message;
            pos.Advance();
            return message switch
            {
                NoteOffMessage noteOff => new SeekNoteResult(SeekNoteStatus.NoteOffFound, noteOff.Channel, noteOff.Key, noteOff.Velocity),
                NoteOnMessage noteOn => new SeekNoteResult(
                    noteOn.Velocity > 0 ? SeekNoteStatus.NoteOnFound : SeekNoteStatus.NoteOffFound, 
                    noteOn.Channel, noteOn.Key, noteOn.Velocity),// iMUSE accepts velocity 0 as note-off
                EndOfTrackMessage => SeekNoteResult.ReachedEndOfTrack,
                _ => SeekNoteResult.NoteNotFound
            };
        }
    }
}
