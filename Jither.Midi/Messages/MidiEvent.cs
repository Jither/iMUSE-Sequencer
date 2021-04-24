using Jither.Midi.Sequencing;

namespace Jither.Midi.Messages
{
    public class MidiEvent : ISchedulable
    {
        private readonly long absoluteTicks;

        public long AbsoluteTicks => absoluteTicks;
        public long Time => absoluteTicks;
        public MidiMessage Message { get; }
        public BeatPosition BeatPosition { get; set; }

        public MidiEvent(long absoluteTicks, MidiMessage message)
        {
            this.absoluteTicks = absoluteTicks;
            Message = message;
        }

        public override string ToString()
        {
            string result = $"({AbsoluteTicks,8}):  {Message}";
            if (BeatPosition != null)
            {
                result = $"{BeatPosition,20} {result}";
            }
            return result;
        }
    }
}
