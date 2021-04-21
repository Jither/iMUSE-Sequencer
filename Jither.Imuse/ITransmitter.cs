using Jither.Midi.Messages;

namespace Jither.Imuse
{
    public interface ITransmitter
    {
        void Init(int ticksPerQuarterNote);
        void Transmit(MidiEvent evt);
        void TransmitImmediate(MidiMessage message);
    }
}
