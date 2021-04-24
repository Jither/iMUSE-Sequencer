using Jither.Midi.Messages;
using System.Linq;

namespace Jither.Imuse
{
    /// <summary>
    /// SequencerPointer stores a location within a single sequencer - track + index of a MIDI event within the track.
    /// </summary>
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
}
