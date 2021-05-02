using Jither.Midi.Files;
using Jither.Midi.Messages;
using System;

namespace Jither.Imuse.Messages
{
    public enum NoOpSignal
    {
        Initialized,
        ReadyForNextBatch
    }

    /// <summary>
    /// Message with no MIDI functionality, used internally for e.g. signalling that more data needs to be buffered.
    /// </summary>
    public class NoOpMessage : MidiMessage
    {
        public override string Name => "noop";

        public override string Parameters => "";

        public NoOpSignal Signal { get; }

        public override int RawMessage => throw new NotImplementedException();

        public NoOpMessage(NoOpSignal signal) : base()
        {
            Signal = signal;
        }

        public override void Write(IMidiWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
