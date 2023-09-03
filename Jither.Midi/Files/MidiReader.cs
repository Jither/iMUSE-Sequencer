using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jither.Midi.Files
{
    public class MidiReader : IDisposable
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

        public MidiReader(Stream stream)
        {
            this.stream = stream;
        }

        private int ReadBytes(int count)
        {
#if DEBUG
            if (count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Attempt to read more bytes ({count}) than fit in buffer ({buffer.Length})");
            }
#endif
            int read = stream.Read(buffer, 0, count);
            if (read < count)
            {
                throw new MidiFileException($"Read beyond end of file, reading bytes.");
            }
            return read;
        }

        public string ReadChunkType()
        {
            ReadBytes(4);
            return Encoding.ASCII.GetString(buffer);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public byte ReadByte()
        {
            int result = stream.ReadByte();
            if (result < 0)
            {
                throw new MidiFileException("Read beyond end of file, reading byte.");
            }
            return (byte)result;
        }

        public sbyte ReadSByte()
        {
            return (sbyte)(ReadByte());
        }

        public byte ReadStatus(int runningStatus)
        {
            byte result = ReadByte();
            if ((result & 0xf0) == 0)
            {
                // We're using running status - backtrack
                stream.Seek(-1, SeekOrigin.Current);
                if (runningStatus < 0)
                {
                    throw new MidiFileException("No status byte at start of track (i.e. attempting to use running status without previous event)");
                }
                return (byte)runningStatus;
            }
            return result;
        }

        public byte[] ReadVariableBytes()
        {
            var length = ReadVLQ();
            byte[] data = new byte[length];
            int read = stream.Read(data, 0, length);
            if (read < length)
            {
                throw new MidiFileException($"Read beyond length of file, reading {length} bytes.");
            }
            return data;
        }

        public uint ReadUint32()
        {
            ReadBytes(4);
            return (uint)(buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3]);
        }

        public ushort ReadUint16()
        {
            ReadBytes(2);
            return (ushort)(buffer[0] << 8 | buffer[1]);
        }

        // Max value of variable length quantity is 0x7ffffff, so int is fine - it will always be positive.
        public int ReadVLQ()
        {
            int result = 0;
            while (true)
            {
                result <<= 7;
                int b = stream.ReadByte();
                if (b < 0)
                {
                    throw new MidiFileException("Read beyond end of file, reading VLQ");
                }
                result |= (byte)(b & 0x7f);
                if ((b & 0x80) == 0)
                {
                    return result;
                }
            }
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
