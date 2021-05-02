using Jither.Midi.Messages;
using System;
using System.Text;

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
        private const int reverbOffset = 6;

        private const int rhythmChannel = 9;
        private const int partCount = 32;

        private const int RealPartBaseAddress = 0x0c000;
        private const int RealPartSize = 0x10;

        private const int ActiveSetupBaseAddress = 0x10000;
        private const int ActiveSetupSize = 0xf6;

        private const int VirtualPartBaseAddress = 0x14000;
        private const int VirtualPartSize = 0x08;

        private const int StoredSetupBaseAddress = 0x20000;
        private const int StoredSetupSize = 0x100;
        private const int StoredSetupCount = 0x20;

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

        public override bool UsesStoredSetup => true;

        public Roland(ITransmitter transmitter) : base(transmitter)
        {
        }

        public override int GetChannelForSlot(int slotIndex)
        {
            int result = slotIndex + 1; // Channels 2-9 (1-8 zero-indexed)
            // Don't stomp on rhythm channel:
            if (result >= rhythmChannel)
            {
                result++;
            }
            return result;
        }

        protected override void Init()
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
            TransmitSysex(displayAddress, display);
            Delay(10);

            Reset();
            Delay(300);

            // Initialize master system settings
            TransmitSysex(systemAddress, initSysString);
            Delay(10);
            // Initialize rhythm keys
            TransmitSysex(rhythmAddress, initRhythmString);
            Delay(10);

            // Initialize rhythm channel volume
            rhythmVolume = 127;
            TransmitControl(rhythmChannel, MidiController.ChannelVolume, rhythmVolume);

            // Initialize pitch bend range for all parts
            address = VirtualPartBaseAddress + pitchBendRangeOffset;
            var pbr = new byte[] { 16 };
            for (int part = 0; part < partCount; part++)
            {
                TransmitSysex(address, pbr);
                address += VirtualPartSize;
                Delay(10);
            }
        }

        private void Reset()
        {
            TransmitSysex(resetAddress, Array.Empty<byte>());
        }

        public override void Close()
        {
            // Immediately send reset - the scheduler isn't running at this point:
            var message = new SysexMessage(GenerateSysex(resetAddress, Array.Empty<byte>()));
            TransmitImmediate(message);
        }

        private void TransmitSysex(int address, byte[] data)
        {
            var message = new SysexMessage(GenerateSysex(address, data));
            TransmitEvent(message);
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
                // MT-32 driver reverses polarity of pan - e.g. -10 means 10 to the *right*
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

        public override void SetReverb(Part part)
        {
            // Added to Roland driver in iMUSE v2
            byte[] reverb = new byte[] { (byte)(part.Reverb != 0 ? 1 : 0) };
            TransmitSysex(GetExternalAddress(part) + reverbOffset, reverb);
            if (part.Slot != null)
            {
                TransmitSysex(GetSlotExternalAddress(part.Slot) + reverbOffset, reverb);
            }
        }

        public override void SetChorus(Part part)
        {
            // Do nothing
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
            buffer[6] = (byte)(part.Reverb != 0 ? 1 : 0);

            TransmitSysex(GetExternalAddress(part), buffer);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Index); // Load external part into slot
            }

            SetModWheel(part);
            SetVolume(part);
            SetPan(part);
            SetSustain(part);
            SetPitchOffset(part);
        }

        // AKA LoadRomSetup
        public override void DoProgramChange(Part part)
        {
            int program = part.Program;

            byte[] buffer = new byte[2];
            buffer[0] = (byte)(program >> 6);
            buffer[1] = (byte)(program & 0x3f);
            TransmitSysex(GetExternalAddress(part), buffer);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Index);
            }
        }

        // AKA DoActiveDump
        public override void ActiveSetup(Part part, byte[] data)
        {
            byte[] buffer = new byte[2];
            buffer[0] = MemoryBank;
            buffer[1] = (byte)part.Index;
            TransmitSysex(GetExternalAddress(part), buffer);

            TransmitSysex(GetPartSetupAddress(part), data);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Index);
            }
        }

        // AKA DoStoredDump
        public override bool StoredSetup(int setupNumber, byte[] data)
        {
            if (setupNumber >= Roland.StoredSetupCount)
            {
                return false;
            }
            TransmitSysex(storedSetupAddresses[setupNumber], data);
            return true;
        }

        // AKA LoadStoredSetup
        public override bool LoadSetup(Part part, int program)
        {
            if (program >= Roland.StoredSetupCount)
            {
                return false;
            }

            byte[] buffer = new byte[2];
            buffer[0] = MemoryBank;
            buffer[1] = (byte)(program + storedSetupStart);
            TransmitSysex(GetExternalAddress(part), buffer);

            if (part.Slot != null)
            {
                TransmitProgramChange(part.Slot.OutputChannel, part.Index);
            }

            return true;
        }

        // AKA DoParamAdjust
        public override bool SetupParam(Part part, int param, int value)
        {
            if (param >= Roland.StoredSetupCount)
            {
                return false;
            }

            byte[] buffer = new byte[1];
            buffer[0] = (byte)value;
            TransmitSysex(GetPartSetupAddress(part) + param, buffer);

            if (part.Slot != null)
            {
                TransmitSysex(GetSlotSetupAddress(part.Slot) + param, buffer);
            }

            return true;
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

        private int GetExternalAddress(Part part)
        {
            return VirtualPartBaseAddress + VirtualPartSize * part.Index;
        }

        private int GetPartSetupAddress(Part part)
        {
            return StoredSetupBaseAddress + StoredSetupSize * part.Index;
        }

        private int GetSlotExternalAddress(Slot slot)
        {
            return RealPartBaseAddress + RealPartSize * slot.Index;
        }

        private int GetSlotSetupAddress(Slot slot)
        {
            return ActiveSetupBaseAddress + ActiveSetupSize * slot.Index;
        }
    }
}
