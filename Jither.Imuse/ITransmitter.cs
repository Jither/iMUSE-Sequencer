using Jither.Midi.Messages;
using System;

namespace Jither.Imuse
{
    public interface ITransmitter : IDisposable
    {
        Action<long> Player { get; set; }

        void Init(int ticksPerQuarterNote);
        void Start();
        void Transmit(MidiEvent evt);
        void TransmitImmediate(MidiMessage message);
    }
}
