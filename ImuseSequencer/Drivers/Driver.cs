using ImuseSequencer.Playback;
using Jither.Midi.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Drivers
{
    public abstract class Driver
    {
        protected readonly OutputDevice output;

        protected Driver(OutputDevice output)
        {
            this.output = output;
        }

        public abstract void Init();
        public abstract void Reset();

        public abstract void StartNote(Part part, int note, int velocity);
        public abstract void StopNote(Part part, int note);
        public abstract void SetVolume(Part part);
        public abstract void SetPan(Part part);
        public abstract void SetPitchOffset(Part part);
        public abstract void SetModWheel(Part part);
        public abstract void SetSustain(Part part);

        public abstract void LoadPart(Part part);
        public abstract void LoadRomSetup(Part part, int value);
        public abstract void DoActiveDump(Part part, byte[] data);
        public abstract void DoStoredDump(int program, byte[] data);
        public abstract void LoadStoredSetup(Part part, int number);
        public abstract void UpdateSetup(Part part);
        public abstract void DoParamAdjust(Part part, int param, int value);

        public abstract void StopAllNotes(Slot slot);

        public abstract void GetSustainNotes(Slot slot, HashSet<int> notes);
    }
}
