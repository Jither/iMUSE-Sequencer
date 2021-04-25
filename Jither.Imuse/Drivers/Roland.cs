using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Drivers
{
    public class Roland : Driver
    {
        private const byte sysexId = 0x41;

        private const int rhythmAddress = 0xc090;
        private const int systemAddress = 0x40000;
        private const int displayAddress = 0x80000;
        private const int resetAddress = 0x1fc000;
        private const int storedSetupStart = 24;

        private const int pitchBendRangeOffset = 4;
        //private const int reverbOffset = 6;

        private const int rhythmChannel = 9;
        private const int partCount = 32;

        public const int RealPartBaseAddress = 0x0c000;
        public const int RealPartSize = 0x10;

        public const int ActiveSetupBaseAddress = 0x10000;
        public const int ActiveSetupSize = 0xf6;

        public const int VirtualPartBaseAddress = 0x14000;
        public const int VirtualPartSize = 0x08;

        public const int StoredSetupBaseAddress = 0x20000;
        public const int StoredSetupSize = 0x100;
        public const int StoredSetupCount = 0x20;

        private readonly int[] storedSetupAddresses = new int[StoredSetupCount];

        private const int MemoryBank = 2;

        private int rhythmVolume = 127;

        private static readonly string initDisplayString = "Lucasfilm Games     ";

        private static readonly byte[] initSysString = new byte[]
        {
            64,								// master tune
            0,								// reverb mode
            4,								// reverb time
            4,								// reverb level
            4, 4, 4, 4, 4, 4, 4, 4, 0,		// partial reserves
            1, 2, 3, 4, 5, 6, 7, 8, 9,		// midi chans
            100,                            // master vol
        };

        private static readonly byte[] initRhythmString = new byte[]
        { // keys 24-34
            64, 100, 7, 0,		// Acou BD
            74, 100, 6, 0,		// Rim Shot
            65, 100, 7, 0,		// Acou SD
            75, 100, 8, 0,		// Hand Clap
            69, 100, 6, 0,		// Elec SD
            68, 100, 11, 0,	    // Low Tom
            81, 100, 5, 0,		// Low Timbale
            67, 100, 8, 0,		// Mid Tom
            80, 100, 7, 0,		// Hi Timbale
            66, 100, 3, 0,		// Hi Tom
            76, 100, 7, 0,      // Cowbell
        };

        public Roland(ITransmitter transmitter) : base(transmitter)
        {
        }

        public override void Init()
        {
            // Gather addresses for stored setup
            int address = StoredSetupBaseAddress + (StoredSetupSize * storedSetupStart);
            for (int i = 0; i < StoredSetupCount; i++)
            {
                storedSetupAddresses[i] = address;
                address += StoredSetupSize;
            }

            // Set display string (just for fun)
            byte[] display = Encoding.ASCII.GetBytes(initDisplayString);
            TransmitSysexImmediate(displayAddress, display);
            
            Reset();

            // Initialize master system settings
            TransmitSysexImmediate(systemAddress, initSysString);
            // Initialize rhythm keys
            TransmitSysexImmediate(rhythmAddress, initRhythmString);

            // Initialize rhythm channel volume
            rhythmVolume = 127;
            TransmitControlImmediate(rhythmChannel, MidiController.ChannelVolume, rhythmVolume);

            // Initialize pitch bend range for all parts
            address = VirtualPartBaseAddress + pitchBendRangeOffset;
            var pbr = new byte[] { 16 };
            for (int part = 0; part < partCount; part++)
            {
                TransmitSysexImmediate(address, pbr);
                address += VirtualPartSize;
            }
        }

        public override void Reset()
        {
            TransmitSysexImmediate(resetAddress, Array.Empty<byte>());
            // TODO: This should actually delay the events (by way of CurrentTick) to be effective...
            Task.Delay(300);
        }

        private void TransmitControl(int channel, MidiController controller, int value)
        {
            var message = ControlChangeMessage.Create(channel, controller, (byte)value);
            TransmitEvent(message);
        }

        private void TransmitProgramChange(int channel, int program)
        {
            var evt = new ProgramChangeMessage(channel, (byte)program);
            TransmitEvent(evt);
        }

        private void TransmitSysex(int address, byte[] data)
        {
            var evt = new SysexMessage(GenerateSysex(address, data));
            TransmitEvent(evt);
        }

        private void TransmitSysexImmediate(int address, byte[] data)
        {
            var message = new SysexMessage(GenerateSysex(address, data));
            TransmitEventImmediate(message);
        }

        private void TransmitControlImmediate(int channel, MidiController controller, int value)
        {
            var message = ControlChangeMessage.Create(channel, controller, (byte)value);
            TransmitEventImmediate(message);
        }

        private static byte[] GenerateSysex(int address, byte[] data)
        {
            byte checksum = 0;

            byte lo_addr = (byte)(address & 0x7f);
            checksum -= lo_addr;
            address >>= 7;

            byte mid_addr = (byte)(address & 0x7f);
            checksum -= mid_addr;
            address >>= 7;

            byte hi_addr = (byte)(address & 0x7f);
            checksum -= hi_addr;
            //address >>= 7;

            byte[] buffer = new byte[data.Length + 9];

            int index = 0;
            buffer[index++] = sysexId;
            buffer[index++] = 0x10;
            buffer[index++] = 0x16;
            buffer[index++] = 0x12;
            buffer[index++] = hi_addr;
            buffer[index++] = mid_addr;
            buffer[index++] = lo_addr;

            foreach (byte b in data)
            {
                checksum -= b;
                buffer[index++] = b;
            }

            buffer[index++] = (byte)(checksum & 0x7f);
            buffer[index++] = 0xf7;

            return buffer;
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
                TransmitControl(part.Slot.OutputChannel, MidiController.ChannelVolume, part.VolumeEffective);
            }
        }

        public override void SetPan(Part part)
        {
            if (part.Slot != null)
            {
                TransmitControl(part.Slot.OutputChannel, MidiController.Pan, (63 - part.PanEffective) & 0x7f);
            }
        }

        public override void SetPitchOffset(Part part)
        {
            // PitchOffset is auto-updated by way of getter evaluation
            if (part.Slot != null)
            {
                TransmitEvent(new PitchBendChangeMessage(part.Slot.OutputChannel, (ushort)((part.PitchOffset << 2) + 0x2000)));
            }
        }

        public override void SetModWheel(Part part)
        {
            if (part.Slot != null)
            {
                TransmitControl(part.Slot.OutputChannel, MidiController.ModWheel, part.ModWheel);
            }
        }

        public override void SetSustain(Part part)
        {
            if (part.Slot != null)
            {
                TransmitControl(part.Slot.OutputChannel, MidiController.Sustain, part.Sustain);
            }
        }

        public override void LoadPart(Part part)
        {
            byte[] buffer = new byte[8];
            buffer[0] = (byte)(part.Program >> 6);
            buffer[1] = (byte)(part.Program & 0x3f);
            buffer[2] = 24; // key shift
            buffer[3] = 50; // fine tune
            buffer[4] = 16; // bender range
            buffer[5] = 0; // assign mode
            buffer[6] = (byte)(part.Reverb ? 1 : 0);

            TransmitSysex(part.ExternalAddress, buffer);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Number); // Load external part into slot
            }

            SetModWheel(part);
            SetVolume(part);
            SetPan(part);
            SetSustain(part);
            SetPitchOffset(part);
        }

        public override void LoadRomSetup(Part part, int program)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)(program >> 6);
            buffer[1] = (byte)(program & 0x3f);
            TransmitSysex(part.ExternalAddress, buffer);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Number);
            }
        }

        // AKA DoActiveDump
        public override void ActiveSetup(Part part, byte[] data)
        {
            byte[] buffer = new byte[2];
            buffer[0] = MemoryBank;
            buffer[1] = (byte)part.Number;
            TransmitSysex(part.ExternalAddress, buffer);

            TransmitSysex(part.PartSetupAddress, data);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Number);
            }
        }

        // AKA DoStoredDump
        public override void StoredSetup(int setupNumber, byte[] data)
        {
            TransmitSysex(storedSetupAddresses[setupNumber], data);
        }

        // AKA LoadStoredSetup
        public override void LoadSetup(Part part, int program)
        {
            byte[] buffer = new byte[2];
            buffer[0] = MemoryBank;
            buffer[1] = (byte)(program + storedSetupStart);
            TransmitSysex(part.ExternalAddress, buffer);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Number);
            }
        }

        public override void UpdateSetup(Part part)
        {
            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Number);
            }
        }

        // AKA DoParamAdjust
        public override void SetupParam(Part part, int param, int value)
        {
            byte[] buffer = new byte[1];
            buffer[0] = (byte)value;
            TransmitSysex(part.PartSetupAddress + param, buffer);

            if (part.Slot != null)
            {
                TransmitSysex(part.Slot.SlotSetupAddress + param, buffer);
            }
        }

        public override void StopAllNotes(Slot slot)
        {
            slot.NoteTable.Clear();

            TransmitControl(slot.OutputChannel, MidiController.Sustain, 0);
            TransmitControl(slot.OutputChannel, MidiController.AllSoundOff, 0);
        }

        public override void GetSustainNotes(Slot slot, HashSet<int> notes)
        {
            notes.UnionWith(slot.NoteTable);
        }

        public override void TransmitSysex(SysexMessage message, PartsCollection parts)
        {
            // TODO: This is a bit roundabout - driver calling back into parts
            // But eventually, all the Roland specific stuff should be handled by the driver, not parts or player...
            int index = 0;
            byte[] data = message.Data;
            byte manufacturerId = data[index++];
            if (manufacturerId != Roland.sysexId)
            {
                logger.Warning("Unknown sysex manufacturer ID");
                return;
            }

            // Length of data minus manufacturer ID and ending F7.
            int count = data.Length - 2;
            int channel = data[index++];
            int setupNumber = channel;

            index += 2; // Skip model and command IDs
            int address = (data[index++] << 14) | (data[index++] << 7) | (data[index++]);
            int offset = address - ActiveSetupBaseAddress;

            switch (count)
            {
                case 8:
                    if (offset < ActiveSetupSize && channel < 16)
                    {
                        parts.SetupParam(channel, offset, data[index++]);
                    }
                    return;
                case 9:
                    if (offset < ActiveSetupSize && channel < 16)
                    {
                        parts.SetupParam(channel, offset, data[index++]);
                        parts.SetupParam(channel, offset + 1, data[index++]);
                    }
                    return;
                case 253:
                    if (offset == 0 && channel < 16)
                    {
                        var setup = data[index..^2];
                        parts.ActiveSetup(channel, setup);
                    }
                    else
                    {
                        var setup = data[index..^2];
                        StoredSetup(setupNumber, setup);
                    }
                    return;
            }

            // No other sysex please - so not calling base
            logger.Warning("Roland driver got unknown/bad Roland sysex");
        }
    }
}
