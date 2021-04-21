using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public interface ITransmitter
    {
        void Init(int ticksPerQuarterNote);
        void Transmit(MidiEvent evt);
        void TransmitImmediate(MidiMessage message);
    }
}
