using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Messages;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Drivers
{
    public abstract class Driver
    {
        protected static readonly Logger logger = LogProvider.Get(nameof(Driver));

        public long CurrentTick { get; set; }
        public abstract bool UsesStoredSetup { get; }
        public abstract int DefaultReverb { get; }
        public abstract int DefaultSlotCount { get; }

        private int ticksPerQuarterNote;

        private readonly ITransmitter transmitter;

        protected Driver(ITransmitter transmitter)
        {
            this.transmitter = transmitter;
        }

        public abstract int GetChannelForSlot(int slotIndex);

        protected abstract void Init();
        public abstract void Close();

        public abstract void StartNote(Part part, int key, int velocity);
        public abstract void StopNote(Part part, int key);

        public abstract void SetVolume(Part part);
        public abstract void SetPan(Part part);
        public abstract void SetPitchOffset(Part part);
        public abstract void SetReverb(Part part);
        public abstract void SetChorus(Part part);
        public abstract void SetModWheel(Part part);
        public abstract void SetSustain(Part part);
        public abstract void DoProgramChange(Part part);

        public abstract void LoadPart(Part part);
        public abstract bool ActiveSetup(Part part, byte[] data);
        public abstract bool StoredSetup(int program, byte[] data);
        public abstract bool LoadSetup(Part part, int number);
        public abstract bool SetupParam(Part part, int param, int value);

        public void Init(int ticksPerQuarterNote)
        {
            this.ticksPerQuarterNote = ticksPerQuarterNote;
            Init();
        }

        protected void Delay(int milliseconds)
        {
            // Hardcoded microseconds per quarternote = 500000. We (hopefully) only use Delay during initialization
            long ticks = milliseconds * 1000 * ticksPerQuarterNote / 500000;
            CurrentTick += ticks;
        }

        protected void TransmitEvent(MidiMessage message)
        {
            var evt = new MidiEvent(CurrentTick, message);
            transmitter.Transmit(evt);
        }

        protected void TransmitImmediate(MidiMessage message)
        {
            transmitter.TransmitImmediate(message);
        }

        public void TransmitNoOp(NoOpSignal signal)
        {
            TransmitEvent(new NoOpMessage(signal));
        }

        protected void TransmitControl(int channel, MidiController controller, int value)
        {
            var message = ControlChangeMessage.Create(channel, controller, (byte)value);
            TransmitEvent(message);
        }

        protected void TransmitProgramChange(int channel, int program)
        {
            var evt = new ProgramChangeMessage(channel, (byte)program);
            TransmitEvent(evt);
        }

        protected void TransmitPitchBend(int channel, ushort value)
        {
            var evt = new PitchBendChangeMessage(channel, value);
            TransmitEvent(evt);
        }

        public void SetTempo(SetTempoMessage tempo)
        {
            TransmitEvent(tempo);
        }

        public void TransmitMeta(MetaMessage message)
        {
            // We pass on meta messages. They can be used in e.g. a MIDI file writer, but ignored if needed in
            // a transmitter outputting to a MIDI device.
            TransmitEvent(message);
        }

        public virtual void TransmitSysex(SysexMessage message, PartsCollection parts)
        {
            TransmitEvent(message);
        }

        public void StopAllNotes(Slot slot)
        {
            slot.NoteTable.Clear();

            TransmitControl(slot.OutputChannel, MidiController.Sustain, 0);
            TransmitControl(slot.OutputChannel, MidiController.AllNotesOff, 0);
        }

        /// <summary>
        /// Retrieves the currently held notes on the given slot. Note that the channel returned for each
        /// each note the Part (input) channel, not the Slot (output) channel.
        /// </summary>
        public void GetSustainNotes(Slot slot, HashSet<SustainedNote> notes)
        {
            notes.UnionWith(slot.NoteTable.Select(n => new SustainedNote(slot.Part.InputChannel, n)));
        }
    }
}
