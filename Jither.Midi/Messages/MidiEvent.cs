using Jither.Midi.Sequencing;

namespace Jither.Midi.Messages
{
    public class MidiEvent : ISchedulable
    {
        private readonly long absoluteTicks;

        public long AbsoluteTicks => absoluteTicks;
        public long Time => absoluteTicks;
        public int DeltaTicks { get; }
        public MidiMessage Message { get; }
        public BeatPosition BeatPosition { get; set; }

        public MidiEvent(long absoluteTicks, int deltaTicks, MidiMessage message)
        {
            this.absoluteTicks = absoluteTicks;
            DeltaTicks = deltaTicks;
            Message = message;
        }

        public override string ToString()
        {
            string result = $"({AbsoluteTicks,8}):  {Message}";
            if (BeatPosition != null)
            {
                result = $"{BeatPosition,10} {result}";
            }
            return result;
        }
    }
}
