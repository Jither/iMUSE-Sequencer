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

        private const int virtualPartBaseAddress = 0x14000;
        private const int virtualPartSize = 0x08;

        private const int pitchBendRangeOffset = 4;
        private const int reverbOffset = 6;

        private const int rhythmChannel = 9;
        private const int partCount = 32;

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

            var addr = virtualPartBaseAddress + pitchBendRangeOffset;
            var pbr = new byte[] { 16 };

            for (int part = 0; part < partCount; part++)
            {
                TransmitSysex(addr, pbr);
                addr += virtualPartSize;
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
    }
}
