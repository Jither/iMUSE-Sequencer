﻿using Jither.Imuse.Parts;
using Jither.Midi.Messages;

namespace Jither.Imuse.Drivers
{
    public class GeneralMidi : Driver
    {
        private const int channelCount = 16;

        // TODO: Use all of these in Roland too
        private readonly int[] currentVolume = new int[channelCount];
        private readonly int[] currentReverb = new int[channelCount];
        private readonly int[] currentPan = new int[channelCount];
        private readonly int[] currentPitchOffset = new int[channelCount];
        private readonly int[] currentModWheel = new int[channelCount];
        private readonly int[] currentSustain = new int[channelCount];
        private readonly int[] currentChorus = new int[channelCount];

        private int rhythmProgram = 0;
        private const int rhythmChannel = 9;
        private int rhythmVolume = 127;

        public override bool UsesStoredSetup => false;

        public override int DefaultReverb => 64;

        // Yes, iMUSE v2 limits General MIDI to 9 slots...
        public override int DefaultSlotCount => 9;

        public GeneralMidi(ITransmitter transmitter) : base(transmitter)
        {
        }

        public override int GetChannelForSlot(int slotIndex)
        {
            // Don't stomp on rhythm channel:
            return slotIndex >= rhythmChannel ? slotIndex + 1 : slotIndex;
        }

        protected override void Init()
        {
            for (int i = 0; i < channelCount; i++)
            {
                // Set pitch bend range:
                TransmitControl(i, MidiController.RpnLSB, 0);
                TransmitControl(i, MidiController.RpnMSB, 0);
                TransmitControl(i, MidiController.DataEntry, 16);

                currentVolume[i] = 127;
                currentReverb[i] = 64;
                currentPan[i] = 0;
                currentPitchOffset[i] = 0;
                currentModWheel[i] = 0;
                currentSustain[i] = 0;
                currentChorus[i] = 0;

                TransmitControl(i, MidiController.ChannelVolume, 127);
                // Yes, iMUSE (at least v2) sets pan to 63 during initialization - then changes it to 64 when part is allocated.
                // May want to just set to 64 here, and reduce the controller changes a bit.
                TransmitControl(i, MidiController.Pan, 63);
                TransmitPitchBend(i, 8192);
                TransmitControl(i, MidiController.ModWheel, 0);
                TransmitControl(i, MidiController.Sustain, 0);
                TransmitControl(i, MidiController.Reverb, 64);
                TransmitControl(i, MidiController.Chorus, 0);
                TransmitControl(i, MidiController.BankSelect, 0);
                TransmitEvent(new AllNotesOffMessage(i));
            }

            rhythmProgram = 0;
            TransmitProgramChange(rhythmChannel, rhythmProgram);
        }

        public override void Close()
        {
            // Immediately send reset - the scheduler isn't running at this point:
            for (int i = 0; i < channelCount; i++)
            {
                TransmitImmediate(new AllNotesOffMessage(i));
            }
        }

        public override void StartNote(Part part, int key, int velocity)
        {
            if (part.Slot != null)
            {
                part.Slot.NoteTable.Add(key);
                TransmitEvent(new NoteOnMessage(part.Slot.OutputChannel, (byte)key, (byte)velocity));
            }
            else if (part.TransposeLocked)
            {
                if (rhythmVolume != part.VolumeEffective)
                {
                    rhythmVolume = part.VolumeEffective;
                    TransmitControl(rhythmChannel, MidiController.ChannelVolume, rhythmVolume);
                }
                // For the rhythm channel part, *bank* indicates the program (if it's ever used)
                if (rhythmProgram != part.Bank)
                {
                    rhythmProgram = part.Bank;
                    TransmitProgramChange(rhythmChannel, rhythmProgram);
                }
                TransmitEvent(new NoteOnMessage(rhythmChannel, (byte)key, (byte)velocity));
            }
        }

        public override void StopNote(Part part, int key)
        {
            if (part.Slot != null)
            {
                part.Slot.NoteTable.Remove(key);
                TransmitEvent(new NoteOffMessage(part.Slot.OutputChannel, (byte)key, 64));
            }
            else if (part.TransposeLocked)
            {
                TransmitEvent(new NoteOffMessage(rhythmChannel, (byte)key, 64));
            }
        }

        public override void SetVolume(Part part)
        {
            if (part.Slot != null)
            {
                int channel = part.Slot.OutputChannel;
                if (part.VolumeEffective != currentVolume[channel])
                {
                    currentVolume[channel] = part.VolumeEffective;
                    TransmitControl(part.Slot.OutputChannel, MidiController.ChannelVolume, part.VolumeEffective);
                }
            }
        }

        public override void SetPan(Part part)
        {
            if (part.Slot != null)
            {
                int channel = part.Slot.OutputChannel;
                if (part.PanEffective != currentPan[channel])
                {
                    currentPan[channel] = part.PanEffective;
                    // GMIDI uses proper pan polarity
                    TransmitControl(part.Slot.OutputChannel, MidiController.Pan, (part.PanEffective - 192) & 0x7f);
                }
            }
        }

        public override void SetPitchOffset(Part part)
        {
            // PitchOffset is auto-updated by way of getter evaluation
            if (part.Slot != null)
            {
                int channel = part.Slot.OutputChannel;
                if (part.PitchOffset != currentPitchOffset[channel])
                {
                    currentPitchOffset[channel] = part.PitchOffset;
                    TransmitEvent(new PitchBendChangeMessage(channel, (ushort)((part.PitchOffset << 2) + 0x2000)));
                }
            }
        }

        public override void SetReverb(Part part)
        {
            if (part.Slot != null)
            {
                int channel = part.Slot.OutputChannel;
                if (part.Reverb != currentReverb[channel])
                {
                    currentReverb[channel] = part.Reverb;
                    TransmitControl(channel, MidiController.Reverb, part.Reverb);
                }
            }
        }

        public override void SetChorus(Part part)
        {
            if (part.Slot != null)
            {
                int channel = part.Slot.OutputChannel;
                if (part.Chorus != currentChorus[channel])
                {
                    currentChorus[channel] = part.Chorus;
                    TransmitControl(channel, MidiController.Chorus, part.Reverb);
                }
            }
        }

        public override void SetModWheel(Part part)
        {
            if (part.Slot != null)
            {
                int channel = part.Slot.OutputChannel;
                if (part.ModWheel != currentModWheel[channel])
                {
                    currentModWheel[channel] = part.ModWheel;
                    TransmitControl(part.Slot.OutputChannel, MidiController.ModWheel, part.ModWheel);
                }
            }
        }

        public override void SetSustain(Part part)
        {
            if (part.Slot != null)
            {
                int channel = part.Slot.OutputChannel;
                if (part.Sustain != currentSustain[channel])
                {
                    currentSustain[channel] = part.Sustain;
                    TransmitControl(part.Slot.OutputChannel, MidiController.Sustain, part.Sustain);
                }
            }
        }

        public override void DoProgramChange(Part part)
        {
            if (part.Slot != null)
            {
                // This is what iMUSE v2 does - reset bank to 0 immediately after setting program.
                if (part.Bank != 0)
                {
                    TransmitControl(part.Slot.OutputChannel, MidiController.BankSelect, part.Bank);
                    TransmitProgramChange(part.Slot.OutputChannel, part.Program);
                    TransmitControl(part.Slot.OutputChannel, MidiController.BankSelect, 0);
                }
                else
                {
                    TransmitProgramChange(part.Slot.OutputChannel, part.Program);
                }
            }
        }

        public override void LoadPart(Part part)
        {
            // Do nothing
        }

        // AKA DoActiveDump
        public override bool ActiveSetup(Part part, byte[] data)
        {
            // Do nothing
            return false;
        }

        // AKA DoStoredDump
        public override bool StoredSetup(int setupNumber, byte[] data)
        {
            // Do nothing
            return false;
        }

        // AKA LoadStoredSetup
        public override bool LoadSetup(Part part, int program)
        {
            // Do nothing
            return false;
        }

        // AKA DoParamAdjust
        public override bool SetupParam(Part part, int param, int value)
        {
            // Do nothing
            return false;
        }

        public override void TransmitSysex(SysexMessage message, PartsCollection parts)
        {
            // GM driver ignores all custom sysex
        }
    }
}
