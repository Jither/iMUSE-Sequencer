using Jither.Logging;
using Jither.Midi.Files;

namespace Jither.Imuse.Files
{
    public class ImuseMidiHeader
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ImuseMidiHeader));

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
                throw new ImuseMidiHeaderException($"Unknown MDhd chunk format, size {size}");
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
