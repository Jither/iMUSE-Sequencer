using Jither.Midi.Files;
using System;
using System.IO;

namespace Jither.Midi.Devices.Windows
{
    public class WindowsMidiStreamWriter : IMidiWriter
    {
        private readonly Stream stream;
        private readonly byte[] buffer = new byte[4];

        private uint previousTicks;

        public uint PreviousTicks => previousTicks;

        public WindowsMidiStreamWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void WriteHeader(long ticks, uint streamId)
        {
            // Less than 0 indicates immediate
            uint deltaTime = ticks >= 0 ? (uint)ticks - previousTicks : previousTicks;
            WriteUint32(deltaTime);
            WriteUint32(streamId);
            if (ticks > previousTicks)
            {
                previousTicks = (uint)ticks;
            }
        }

        public void WriteEvent(int value, int flags)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)((value >> 24) | flags);
            stream.Write(buffer, 0, 4);
        }

        public void WriteNoOp(int data)
        {
            WriteEvent(data, WinApiConstants.MEVT_NOP | WinApiConstants.MEVT_CALLBACK);
        }

        private void WriteUint32(uint value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            stream.Write(buffer, 0, 4);
        }

        public void WriteShortMessage(byte status, byte value1, byte value2)
        {
            buffer[0] = status;
            buffer[1] = value1;
            buffer[2] = value2;
            buffer[3] = WinApiConstants.MEVT_SHORTMSG;
            stream.Write(buffer, 0, 4);
        }

        public void WriteShortMessage(byte status, byte value)
        {
            WriteShortMessage(status, value, 0);
        }

        public void WriteShortMessage(byte status, ushort value)
        {
            WriteShortMessage(status, (byte)(value & 0x7f), (byte)((value >> 7) & 0x7f));
        }

        public void WriteSysex(byte status, byte[] data)
        {
            int fullLength = data.Length + 1;
            WriteEvent(fullLength, WinApiConstants.MEVT_LONGMSG);

            stream.WriteByte(status);
            stream.Write(data, 0, data.Length);

            int padding = (4 - (fullLength % 4)) % 4;
            for (int i = 0; i < padding; i++)
            {
                stream.WriteByte(0);
            }
        }

        public void WriteMeta(byte status, byte type, byte[] data)
        {
            switch (type) // SetTempo
            {
                case 0x51:
                    WriteEvent((data[0] << 16) | (data[1] << 8) | data[2], WinApiConstants.MEVT_TEMPO);
                    break;
                default:
                    throw new NotImplementedException($"WriteMeta for type {type} not implemented.");
            }
        }
    }
}
