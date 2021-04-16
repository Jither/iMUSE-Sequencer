using ImuseSequencer.Drivers;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    // TODO: Make base class - this is Roland-specific
    public class Part
    {
        // TODO: Move constant elsewhere...
        public const int OmniChannel = 16;

        private readonly Driver driver;
        private Player player;
        private Slot slot;
        public int ExternalAddress { get; }
        public int StoredSetupAddress { get; }

        public int Number { get; private set; }
        public int Channel { get; private set; }
        public bool Enabled { get; private set; }
        public int PriorityOffset { get; private set; }
        public int Volume { get; private set; }
        public int Pan { get; private set; }
        public int Transpose { get; private set; }
        public int Detune { get; private set; }
        public bool TransposeLocked { get; private set; }
        public int ModWheel { get; private set; }
        public int Sustain { get; private set; }
        public int PitchbendRange { get; private set; }
        public int Pitchbend { get; private set; }
        public bool Reverb { get; private set; }
        public int Program { get; private set; }

        public bool IsInUse => player != null;
        public Slot Slot => slot;

        public int PriorityEffective => Math.Clamp(player?.Priority ?? 0 + PriorityOffset, 0, 255);
        public int VolumeEffective => (player.EffectiveVolume * (Volume + 1)) >> 7;
        public int PanEffective => Math.Clamp(player.Pan + Pan, -64, 63);
        public int TransposeEffective => TransposeLocked ? 0 : Math.Clamp(player.Transpose + Transpose, -12, 12);
        public int DetuneEffective => Math.Clamp(player.Detune + Detune, -128, 127);
        public int PitchOffset => Math.Clamp(Pitchbend + DetuneEffective + (TransposeEffective << 7), -0x800, 0x7ff);

        public Part(int number, Driver driver)
        {
            this.driver = driver;
            Number = number;
            ExternalAddress = Roland.VirtualPartBaseAddress + Roland.VirtualPartSize * number;
            StoredSetupAddress = Roland.StoredSetupBaseAddress + Roland.StoredSetupSize * number;
        }

        public void LinkPlayer(Player player)
        {
            this.player = player;
        }

        public void UnlinkPlayer()
        {
            this.player = null;
        }

        public void LinkSlot(Slot slot)
        {
            this.slot = slot;
        }

        public void UnlinkSlot()
        {
            this.slot = null;
        }

        public void Alloc(ImuseAllocPart alloc)
        {
            Channel = alloc.Channel;
            Enabled = alloc.Enabled;
            PriorityOffset = alloc.PriorityOffset;
            Volume = alloc.Volume;
            Pan = alloc.Pan;
            Transpose = alloc.Transpose;
            TransposeLocked = alloc.TransposeLocked;
            Detune = alloc.Detune;
            ModWheel = 0;
            Sustain = 0;
            PitchbendRange = alloc.PitchBendRange;
            Pitchbend = 0;
            Reverb = alloc.Reverb;
            Program = alloc.Program;
        }

        public void StartNote(int note, int velocity)
        {
            driver.StartNote(this, note, velocity);
        }

        public void StopNote(int note, int velocity)
        {
            driver.StopNote(this, note, velocity);
        }

        public void StopAllNotes()
        {
            if (slot != null)
            {
                driver.StopAllNotes(slot);
            }
        }

        public void StopAllSustains()
        {
            if (Sustain != 0)
            {
                Sustain = 0;
                driver.SetSustain(this);
            }
        }

        // TODO: GetSustainNotes

        // TODO: DecodeCustomSysex

        public void DoProgramChange(int value)
        {
            Program = value;
            driver.LoadRomSetup(this, value);
        }

        public void DoActiveDump(byte[] data)
        {
            driver.DoActiveDump(this, data);
        }

        public void DoStoredDump(int number, byte[] data)
        {
            if (number < Roland.StoredSetupCount)
            {
                driver.DoStoredDump(number, data);
            }
        }

        public void LoadSetup(int number)
        {
            if (number < Roland.StoredSetupCount)
            {
                driver.LoadStoredSetup(this, number);
            }
        }

        public void DoParamAdjust(int number, int value)
        {
            driver.DoParamAdjust(this, number, value);
        }

        public void SetPriorityOffset(int priorityOffset)
        {
            PriorityOffset = priorityOffset;
        }

        public void SetVolume(int volume)
        {
            Volume = volume;
            driver.SetVolume(this);
        }

        public void SetPan(int pan)
        {
            Pan = pan;
            driver.SetPan(this);
        }

        public void SetTranspose(int transpose)
        {
            Transpose = transpose;
            driver.SetPitchOffset(this);
        }

        public void SetDetune(int detune)
        {
            Detune = detune;
            driver.SetPitchOffset(this);
        }

        public void SetEnabled(bool state)
        {
            Enabled = state;
            if (!state && slot != null)
            {
                driver.StopAllNotes(slot);
            }
        }

        public void SetModWheel(int value)
        {
            ModWheel = value;
            driver.SetModWheel(this);
        }

        public void SetSustain(int value)
        {
            Sustain = value;
            driver.SetSustain(this);
        }

        public void SetPitchbend(int value)
        {
            Pitchbend = (value * PitchbendRange) >> 6;
            driver.SetPitchOffset(this);
        }
    }
}
