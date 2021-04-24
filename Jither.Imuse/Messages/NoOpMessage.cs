using Jither.Midi.Files;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Messages
{
    /// <summary>
    /// Message with no MIDI functionality, used internally for e.g. signalling that more data needs to be buffered.
    /// </summary>
    public class NoOpMessage : MidiMessage
    {
        public override string Name => "noop";

        public override string Parameters => "";

        public override int RawMessage => throw new NotImplementedException();

        public override void Write(IMidiWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
