using ImuseSequencer.Playback;
using Jither.Midi.Devices;
using Jither.Midi.Messages;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Drivers
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

        private static readonly ushort[] bitmasks = new ushort[]
        {
            0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0040, 0x0080,
            0x0100, 0x0200, 0x0400, 0x0800, 0x1000, 0x2000, 0x4000, 0x8000
        };

        public Roland(OutputDevice output) : base(output)
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
            TransmitSysex(displayAddress, display);
            
            Reset();

            // Initialize master system settings
            TransmitSysex(systemAddress, initSysString);
            // Initialize rhythm keys
            TransmitSysex(rhythmAddress, initRhythmString);

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
            }
        }

        public override void Reset()
        {
            TransmitSysex(resetAddress, Array.Empty<byte>());
            Task.Delay(300);
        }

        private void TransmitNoteOn(int channel, int note, int velocity)
        {
            output.SendMessage(new NoteOnMessage(channel, (byte)note, (byte)velocity));
        }

        private void TransmitNoteOff(int channel, int note, int velocity)
        {
            // Original iMUSE always sends 64 for note-off velocity
            output.SendMessage(new NoteOffMessage(channel, (byte)note, (byte)velocity));
        }

        private void TransmitControl(int channel, MidiController controller, int value)
        {
            output.SendMessage(ControlChangeMessage.Create(channel, controller, (byte)value));
        }

        private void TransmitProgramChange(int channel, int program)
        {
            output.SendMessage(new ProgramChangeMessage(channel, (byte)program));
        }

        private void TransmitPitchbend(int channel, int pitchbend)
        {
            output.SendMessage(new PitchBendChangeMessage(channel, (ushort)pitchbend));
        }

        private void TransmitSysex(int address, byte[] data)
        {
            output.SendMessage(new SysexMessage(GenerateSysex(address, data)));
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

        public override void StartNote(Part part, int note, int velocity)
        {
            if (part.Slot != null)
            {
                part.Slot.NoteTable[note >> 4] |= bitmasks[note & 0x0f];
                TransmitNoteOn(part.Slot.OutputChannel, note, velocity);
            }
            else if (part.TransposeLocked)
            {
                if (rhythmVolume != part.VolumeEffective)
                {
                    rhythmVolume = part.VolumeEffective;
                    TransmitControl(rhythmChannel, MidiController.ChannelVolume, rhythmVolume);
                }
                TransmitNoteOn(rhythmChannel, note, velocity);
            }
        }

        public override void StopNote(Part part, int note, int velocity)
        {
            if (part.Slot != null)
            {
                part.Slot.NoteTable[note >> 4] &= (ushort)(~bitmasks[note & 0x0f]);
                TransmitNoteOff(part.Slot.OutputChannel, note, velocity);
            }
            else if (part.TransposeLocked)
            {
                TransmitNoteOff(rhythmChannel, note, velocity);
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
                TransmitPitchbend(part.Slot.OutputChannel, (part.PitchOffset << 2) + 0x2000);
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

        public override void DoActiveDump(Part part, byte[] data)
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

        public override void DoStoredDump(int program, byte[] data)
        {
            TransmitSysex(storedSetupAddresses[program], data);
        }

        public override void LoadStoredSetup(Part part, int program)
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

        public override void DoParamAdjust(Part part, int number, int value)
        {
            byte[] buffer = new byte[1];
            buffer[0] = (byte)value;
            TransmitSysex(part.PartSetupAddress + number, buffer);

            if (part.Slot != null)
            {
                TransmitSysex(part.Slot.SlotSetupAddress + number, buffer);
            }
        }

        public override void StopAllNotes(Slot slot)
        {
            for (int i = 0; i < 8; i++)
            {
                slot.NoteTable[i] = 0;
            }

            TransmitControl(slot.OutputChannel, MidiController.Sustain, 0);
            TransmitControl(slot.OutputChannel, MidiController.AllSoundOff, 0);
        }
    }
}
