using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse
{
    public class NullTransmitter : ITransmitter
    {
        public ImuseEngine Engine { get; set; }

        public string OutputName => "NULL";

        public void Init(int ticksPerQuarterNote)
        {
        }

        public void Start()
        {
        }

        public void Transmit(MidiEvent evt)
        {
        }

        public void TransmitImmediate(MidiMessage message)
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
