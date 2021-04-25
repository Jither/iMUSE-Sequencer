using Jither.Midi.Messages;
using System;

namespace Jither.Imuse
{
    /// <summary>
    /// Transmitters handle the final output of iMUSE. This allows directing that output to e.g. a physical MIDI device
    /// or write it to a file.
    /// </summary>
    public interface ITransmitter : IDisposable
    {
        ImuseEngine Engine { get; set; }

        void Init(int ticksPerQuarterNote);
        void Start();
        void Transmit(MidiEvent evt);
        void TransmitImmediate(MidiMessage message);
    }
}
