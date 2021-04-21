using ImuseSequencer.Playback;
using Jither.Logging;
using Jither.Midi.Devices;
using Jither.Midi.Messages;
using Jither.Midi.Sequencing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Drivers
{
    public abstract class Driver
    {
        protected static readonly Logger logger = LogProvider.Get(nameof(Driver));

        public long CurrentTick { get; set; }

        protected long previousTick;
        private readonly ITransmitter transmitter;

        protected Driver(ITransmitter transmitter)
        {
            this.transmitter = transmitter;
        }

        public abstract void Init();
        public abstract void Reset();

        public abstract void StartNote(Part part, int key, int velocity);
        public abstract void StopNote(Part part, int key);

        public abstract void SetVolume(Part part);
        public abstract void SetPan(Part part);
        public abstract void SetPitchOffset(Part part);
        public abstract void SetModWheel(Part part);
        public abstract void SetSustain(Part part);

        public abstract void LoadPart(Part part);
        public abstract void LoadRomSetup(Part part, int program);
        public abstract void ActiveSetup(Part part, byte[] data);
        public abstract void StoredSetup(int program, byte[] data);
        public abstract void LoadSetup(Part part, int number);
        public abstract void UpdateSetup(Part part);
        public abstract void SetupParam(Part part, int param, int value);

        public abstract void StopAllNotes(Slot slot);

        public abstract void GetSustainNotes(Slot slot, HashSet<int> notes);

        protected void TransmitEvent(MidiMessage message)
        {
            var evt = new MidiEvent(CurrentTick, (int)(CurrentTick - previousTick), message);
            transmitter.Transmit(evt);
            previousTick = CurrentTick;
        }

        protected void TransmitEventImmediate(MidiMessage message)
        {
            transmitter.TransmitImmediate(message);
        }

        public void SetTempo(MidiMessage tempo)
        {
            TransmitEvent(tempo);
        }

        public void Sysex(SysexMessage message)
        {
            TransmitEvent(message);
        }

    }
}
