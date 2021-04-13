using System.Collections.Generic;

namespace Jither.Midi.Parsing
{
    public class MidiTrack
    {
        public uint Index { get; }
        public IReadOnlyList<MidiEvent> Events { get; }

        public MidiTrack(uint index, List<MidiEvent> events)
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
