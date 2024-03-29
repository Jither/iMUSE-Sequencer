﻿using System;
using System.IO;
using System.Text;

namespace Jither.Midi.Files
{
    public interface IMidiWriter
    {
        void WriteShortMessage(byte status, byte value1, byte value2);
        void WriteShortMessage(byte status, byte value);
        void WriteShortMessage(byte status, ushort value);
        void WriteSysex(byte status, byte[] data);
        void WriteMeta(byte status, byte type, byte[] data);
    }

    public class MidiFileWriter : IMidiWriter, IDisposable
    {
        private Stream stream;
        private bool disposed;
        private readonly byte[] buffer = new byte[4];

        public long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }

        public long Length => stream.Length;

        public MidiFileWriter(Stream stream)
        {
            this.stream = stream;
        }

        private void WriteBytes(byte[] bytes, int count = -1)
        {
            stream.Write(bytes, 0, count > 0 ? count : bytes.Length);
        }

        public void WriteChunkType(string type)
        {
            if (type.Length != 4)
            {
                throw new MidiFileException($"Attempt to write chunk type with length != 4: {type}");
            }
            WriteBytes(Encoding.ASCII.GetBytes(type));
        }

        public void WriteByte(byte value)
        {
            stream.WriteByte(value);
        }

        public void WriteSByte(sbyte value)
        {
            stream.WriteByte((byte)value);
        }

        public void WriteVariableBytes(byte[] bytes)
        {
            WriteVLQ(bytes.Length);
            WriteBytes(bytes);
        }

        public void WriteUint32(uint value)
        {
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)(value);
            WriteBytes(buffer);
        }

        public void WriteUint16(ushort value)
        {
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)(value);
            WriteBytes(buffer, 2);
        }

        // Max value of variable length quantity is 0x7ffffff, so int is fine - it will always be positive.
        public void WriteVLQ(int value)
        {
            int count = 0;
            buffer[count++] = (byte)(value & 0x7f);
            value >>= 7;
            while (value > 0)
            {
                buffer[count++] = (byte)(value | 0x80);
                value >>= 7;
            }

            // count is now the number of bytes required, and the buffer contains the bytes in reverse order
            while (count > 0)
            {
                count--;
                stream.WriteByte(buffer[count]);

            }
        }

        public void WriteShortMessage(byte status, byte value1, byte value2)
        {
            WriteByte(status);
            WriteByte(value1);
            WriteByte(value2);
        }

        public void WriteShortMessage(byte status, ushort value)
        {
            WriteByte(status);
            WriteByte((byte)(value & 0x7f));
            WriteByte((byte)((value & 0x3f80) >> 7));
        }

        public void WriteShortMessage(byte status, byte value)
        {
            WriteByte(status);
            WriteByte(value);
        }

        public void WriteSysex(byte status, byte[] data)
        {
            WriteByte(status);
            WriteVariableBytes(data);
        }

        public void WriteMeta(byte status, byte type, byte[] data)
        {
            WriteByte(status);
            WriteByte(type);
            WriteVariableBytes(data);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                stream = null;
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
