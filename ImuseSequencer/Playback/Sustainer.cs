using Jither.Logging;
using Jither.Midi.Messages;
using Jither.Midi.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public enum SustainDefStatus
    {
        Unused,
        Pending,
        Active
    }

    public class SustainDef
    {
        public int Note { get; }
        public int Channel { get; }
        public Sequencer Sequencer { get; }
        public long SustainTicks { get; }
        public int TickCount { get; set; }

        public SustainDef(Sequencer sequencer, int note, int channel, long sustainTime)
        {
            Sequencer = sequencer;
            Note = note;
            Channel = channel;
            SustainTicks = sustainTime;
            TickCount = 0;
        }
    }

    public enum SeekNoteStatus
    {
        NoteOnFound,
        NoteOffFound,
        NoteNotFound
    }

    public class SeekNoteResult
    {
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

    public class SequencerPointer
    {
        public MidiTrack Track { get; }
        public int EventIndex { get; private set; }
        public MidiEvent Event => EventIndex < Track.Events.Count ? Track.Events[EventIndex] : null;
        public long NextEventTick => Event?.AbsoluteTicks ?? -1;

        public SequencerPointer(MidiTrack track, int eventIndex)
        {
            Track = track;
            EventIndex = eventIndex;
        }

        public void Advance()
        {
            EventIndex++;
        }
    }

    // TODO: Unlike iMUSE, which only allows 24 sustained notes at a time, this implementation is unlimited.
    public class Sustainer
    {
        private static readonly Logger logger = LogProvider.Get(nameof(Sustainer));

        private readonly List<SustainDef> activeSustainDefs = new();
        private readonly HashSet<int> noteTable = new();

        public Sustainer()
        {
        }

        public void Tick()
        {
            for (int i = activeSustainDefs.Count - 1; i >= 0; i--)
            {
                var sustainDef = activeSustainDefs[i];
                sustainDef.TickCount++;
                if (sustainDef.TickCount >= sustainDef.SustainTicks)
                {
                    var message = new NoteOffMessage(sustainDef.Channel, (byte)sustainDef.Note, 0);
                    logger.Debug($"Stopping sustained note {message}");
                    sustainDef.Sequencer.Parts.HandleEvent(message); // Velocity doesn't matter. Driver will replace it.
                    activeSustainDefs.RemoveAt(i);
                }
            }
        }

        public void AnalyzeSustain(Sequencer sequencer, MidiTrack oldTrack, int oldNextIndex, MidiTrack newTrack, int newNextIndex, long newSustainTicks)
        {
            var sustainDefs = new List<SustainDef>();
            noteTable.Clear();
            sequencer.Parts.GetSustainNotes(noteTable);

            long sustainTicks = sequencer.NextEventTick - sequencer.CurrentTick;

            if (sustainTicks < 0)
            {
                sustainTicks = 0;
            }

            int noteCount = noteTable.Count;
            var pos = new SequencerPointer(oldTrack, oldNextIndex);
            while (noteTable.Count > 0)
            {
                var result = SeekNote(pos);
                if (result.Status == SeekNoteStatus.NoteOffFound)
                {
                    if (noteTable.Contains(result.Note))
                    {
                        noteTable.Remove(result.Note);
                        sustainDefs.Add(new SustainDef(sequencer, result.Note, result.Channel, sustainTicks));
                    }
                }
                sustainTicks = pos.NextEventTick - sequencer.CurrentTick;
            }

            long maxTicks = 0;
            if (sustainDefs.Count > 0)
            {
                maxTicks = sustainDefs.Max(s => s.SustainTicks);
            }

            // Now search from the new position, and ensure note-offs don't stump on its note-ons (i.e., cut them off).
            sustainTicks = newSustainTicks;
            pos = new SequencerPointer(newTrack, newNextIndex);
            while (sustainTicks < maxTicks)
            {
                var result = SeekNote(pos);
                if (result.Status == SeekNoteStatus.NoteOnFound)
                {
                    var sustainDef = sustainDefs.Find(s => s.Note == result.Note && s.Channel == result.Channel);
                    if (sustainDef != null && sustainTicks < sustainDef.SustainTicks)
                    {
                        sustainDefs.Remove(sustainDef);
                    }
                }
                sustainTicks = pos.NextEventTick - newSustainTicks;
            }

            activeSustainDefs.AddRange(sustainDefs);
        }

        private SeekNoteResult SeekNote(SequencerPointer pos)
        {
            var message = pos.Event.Message;
            pos.Advance();
            return message switch
            {
                NoteOffMessage noteOff => new SeekNoteResult(SeekNoteStatus.NoteOffFound, noteOff.Channel, noteOff.Key, noteOff.Velocity),
                NoteOnMessage noteOn => new SeekNoteResult(noteOn.Velocity > 0 ? SeekNoteStatus.NoteOnFound : SeekNoteStatus.NoteOffFound, noteOn.Channel, noteOn.Key, noteOn.Velocity),// iMUSE accepts velocity 0 as note-off
                _ => new SeekNoteResult(SeekNoteStatus.NoteNotFound, 0, 0, 0),
            };
        }
    }
}
