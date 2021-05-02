using Jither.Midi.Helpers;

namespace Jither.Imuse
{
    /// <summary>
    /// Represents a note active on a channel. Used by <see cref="Sustainer"/> when querying sustained notes from Parts.
    /// </summary>
    public struct SustainedNote
    {
        public int Channel { get; }
        public int Key { get; }

        public SustainedNote(int channel, int key)
        {
            Channel = channel;
            Key = key;
        }

        public override string ToString()
        {
            return $"Channel {Channel}, Key {MidiHelper.NoteNumberToName(Key)} ({Key})";
        }
    }
}
