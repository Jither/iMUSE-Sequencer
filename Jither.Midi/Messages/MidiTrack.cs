using Jither.Midi.Messages;
using System.Collections.Generic;

namespace Jither.Midi.Messages
{
    public class MidiTrack
    {
        public int Index { get; }
        public IReadOnlyList<MidiEvent> Events { get; }

        public MidiTrack(int index, List<MidiEvent> events)
        {
            Index = index;
            Events = events;
        }

        public override string ToString()
        {
            return $"Track {Index}";
        }
    }
}
