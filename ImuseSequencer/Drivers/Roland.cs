using ImuseSequencer.Playback;
using Jither.Midi.Devices;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private const int pitchBendRangeOffset = 4;
        private const int reverbOffset = 6;

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

        public Roland(OutputDevice output) : base(output)
        {

        }

        public override void Init()
        {
            byte[] display = Encoding.ASCII.GetBytes(initDisplayString);
            TransmitSysex(displayAddress, display);
            Reset();

            TransmitSysex(systemAddress, initSysString);
            TransmitSysex(rhythmAddress, initRhythmString);

            output.SendMessage(ControlChangeMessage.Create(rhythmChannel, MidiController.ChannelVolume, 127));

            var addr = VirtualPartBaseAddress + pitchBendRangeOffset;
            var pbr = new byte[] { 16 };

            for (int part = 0; part < partCount; part++)
            {
                TransmitSysex(addr, pbr);
                addr += VirtualPartSize;
            }
        }

        public override void Reset()
        {
            TransmitSysex(resetAddress, Array.Empty<byte>());
            Task.Delay(300);
        }

        private void TransmitSysex(int address, byte[] data)
        {
            output.SendMessage(new SysexMessage(GenerateSysex(address, data)));
        }

        private void TransmitProgramChange(int channel, int program)
        {
            output.SendMessage(new ProgramChangeMessage(channel, (byte)program));
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
            throw new NotImplementedException();
        }

        public override void StopNote(Part part, int note, int velocity)
        {
            throw new NotImplementedException();
        }

        public override void SetVolume(Part part)
        {
            throw new NotImplementedException();
        }

        public override void SetPan(Part part)
        {
            throw new NotImplementedException();
        }

        public override void SetPitchOffset(Part part)
        {
            throw new NotImplementedException();
        }

        public override void SetModWheel(Part part)
        {
            throw new NotImplementedException();
        }

        public override void SetSustain(Part part)
        {
            throw new NotImplementedException();
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
                TransmitProgramChange(part.Slot.Channel, part.Number); // Load external part into slot
            }

            SetModWheel(part);
            SetVolume(part);
            SetPan(part);
            SetSustain(part);
            SetPitchOffset(part);
        }

        public override void LoadRomSetup(Part part, int value)
        {
            throw new NotImplementedException();
        }

        public override void DoActiveDump(Part part, byte[] data)
        {
            throw new NotImplementedException();
        }

        public override void DoStoredDump(int number, byte[] data)
        {
            throw new NotImplementedException();
        }

        public override void LoadStoredSetup(Part part, int number)
        {
            throw new NotImplementedException();
        }

        public override void UpdateSetup(Part part)
        {
            throw new NotImplementedException();
        }

        public override void DoParamAdjust(Part part, int number, int value)
        {
            throw new NotImplementedException();
        }

        public override void StopAllNotes(Slot slot)
        {
            throw new NotImplementedException();
        }
    }
}
