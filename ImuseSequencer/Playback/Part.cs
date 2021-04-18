using ImuseSequencer.Drivers;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;

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
        public int PartSetupAddress { get; }

        public int Number { get; private set; }
        
        /// <summary>
        /// The channel whose input messages this part will handle.
        /// </summary>
        public int InputChannel { get; private set; }
        public bool Enabled { get; private set; }
        public int PriorityOffset { get; private set; }
        public int Volume { get; private set; }
        public int Pan { get; private set; }
        public int Transpose { get; private set; }
        public int Detune { get; private set; }

        /// <summary>
        /// <c>true</c> for percussion parts, <c>false</c> for other instruments.
        /// </summary>
        public bool TransposeLocked { get; private set; }

        public int ModWheel { get; private set; }
        public int Sustain { get; private set; }
        public int PitchbendRange { get; private set; }
        public int Pitchbend { get; private set; }
        public bool Reverb { get; private set; }
        public int Program { get; private set; }

        /// <summary>
        /// Indicates whether this part is currently assigned to a player.
        /// </summary>
        public bool IsInUse => player != null;

        /// <summary>
        /// The slot that currently handles output from this part. This may be null, if all slots are occupied by
        /// parts with higher priority - and is also null for percussion parts.
        /// </summary>
        public Slot Slot => slot;

        // Effective values (typically a combination of player's value and part's value)
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
            PartSetupAddress = Roland.StoredSetupBaseAddress + Roland.StoredSetupSize * number;
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
            InputChannel = alloc.Channel;
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

        public void HandleEvent(ChannelMessage message)
        {
            switch (message)
            {
                case NoteOnMessage noteOn:
                    driver.StartNote(this, noteOn.Key, noteOn.Velocity);
                    break;
                case NoteOffMessage noteOff:
                    driver.StopNote(this, noteOff.Key);
                    break;
                case ControlChangeMessage controlChange:
                    switch (controlChange.Controller)
                    {
                        case MidiController.ModWheel:
                            ModWheel = controlChange.Value;
                            driver.SetModWheel(this);
                            break;
                        case MidiController.ChannelVolume:
                            SetVolume(controlChange.Value);
                            break;
                        case MidiController.Pan:
                            Pan = controlChange.Value - 0x40; // Center
                            driver.SetPan(this);
                            break;
                        case MidiController.Sustain:
                            Sustain = controlChange.Value;
                            driver.SetSustain(this);
                            break;
                    }
                    break;
                case ProgramChangeMessage programChange:
                    DoProgramChange(programChange.Program);
                    break;
                case PitchBendChangeMessage pitchBend:
                    int bender = pitchBend.Bender - 0x2000; // Center around 0
                    Pitchbend = (bender * PitchbendRange) >> 6;
                    driver.SetPitchOffset(this);
                    break;
                default:
                    throw new ArgumentException($"Unexpected message to part: {message}");
            }
        }

        public void HandleEvent(ImuseMessage message)
        {
            switch (message)
            {
                case ImuseActiveSetup activeSetup:
                    driver.ActiveSetup(this, activeSetup.Setup);
                    break;
                case ImuseStoredSetup storedSetup:
                    // Should be done... elsewhere - not part-related
                    if (storedSetup.SetupNumber < Roland.StoredSetupCount)
                    {
                        driver.StoredSetup(storedSetup.SetupNumber, storedSetup.Setup);
                    }
                    break;
                case ImuseSetupBank:
                    // Not used by Roland
                    break;
                case ImuseSystemParam:
                    // Not used by Roland
                    break;
                case ImuseSetupParam setupParam:
                    if (setupParam.Number < Roland.StoredSetupCount)
                    {
                        driver.SetupParam(this, setupParam.Number, setupParam.Value);
                    }
                    break;
                case ImuseMarker:
                    break;
                case ImuseLoadSetup loadSetup:
                    if (loadSetup.SetupNumber < Roland.StoredSetupCount)
                    {
                        driver.LoadSetup(this, loadSetup.SetupNumber);
                    }
                    break;

            }
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

        public void GetSustainNotes(HashSet<int> notes)
        {
            if (slot != null)
            {
                driver.GetSustainNotes(slot, notes);
            }
        }

        // TODO: DecodeCustomSysex

        public void SetPriorityOffset(int priorityOffset)
        {
            PriorityOffset = priorityOffset;
        }

        public void SetVolume(int volume)
        {
            Volume = volume;
            driver.SetVolume(this);
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

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (!enabled && slot != null)
            {
                driver.StopAllNotes(slot);
            }
        }

        public void DoProgramChange(int program)
        {
            Program = program;
            driver.LoadRomSetup(this, program);
        }
    }
}
