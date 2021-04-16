using Jither.Logging;

namespace Jither.Midi.Parsing
{
    public class ImuseMidiHeader
    {
        private static Logger logger = LogProvider.Get(nameof(ImuseMidiHeader));

        public int Version { get; }
        public int Priority { get; }
        public int Volume { get; }
        public int Pan { get; }
        public int Transpose { get; }
        public int Detune { get; }
        public int Speed { get; }

        public ImuseMidiHeader(MidiReader reader, uint size)
        {
            if (size != 8)
            {
                logger.Warning($"Unknown MDhd chunk format, size {size}");
                return;
            }

            Version = reader.ReadUint16();
            Priority = reader.ReadByte();
            Volume = reader.ReadByte();
            Pan = reader.ReadSByte();
            Transpose = reader.ReadSByte();
            Detune = reader.ReadSByte();
            Speed = reader.ReadByte();
        }

        public override string ToString()
        {
            return $"Version {Version}, Priority {Priority}, Volume {Volume}, Pan {Pan}, Transpose {Transpose}, Detune {Detune}, Speed {Speed}";
        }
    }
}
