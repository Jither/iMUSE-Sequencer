using FluentAssertions;
using Jither.Midi.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jither.Midi.Files
{
    public class MidiWriterTests : IDisposable
    {
        private MemoryStream stream;
        private MidiFileWriter writer;

        public MidiWriterTests()
        {
            stream = new MemoryStream();
            writer = new MidiFileWriter(stream);
        }

        public void Dispose()
        {
            writer.Dispose();
            stream.Dispose();
        }

        [Theory]
        [InlineData("MThd", new byte[] { 0x4d, 0x54, 0x68, 0x64 })]
        [InlineData("MTrk", new byte[] { 0x4d, 0x54, 0x72, 0x6b })]
        public void Writes_chunk_type(string type, byte[] expected)
        {
            writer.WriteChunkType(type);

            stream.ToArray().Should().Equal(expected);
        }

        [Theory]
        [InlineData(0x12345678, new byte[] { 0x12, 0x34, 0x56, 0x78 })]
        [InlineData(0xffeeddcc, new byte[] { 0xff, 0xee, 0xdd, 0xcc })]
        public void Writes_big_endian_integer(uint input, byte[] expected)
        {
            writer.WriteUint32(input);

            stream.ToArray().Should().Equal(expected);
        }

        [Theory]
        [InlineData(0x1234, new byte[] { 0x12, 0x34 })]
        [InlineData(0xffee, new byte[] { 0xff, 0xee })]
        public void Writes_big_endian_short(ushort input, byte[] expected)
        {
            writer.WriteUint16(input);

            stream.ToArray().Should().Equal(expected);
        }

        [Theory]
        [InlineData(0x00000000, new byte[] { 0x00 })]
        [InlineData(0x0000007f, new byte[] { 0x7f })]
        [InlineData(0x00000080, new byte[] { 0x81, 0x00 })]
        [InlineData(0x00002000, new byte[] { 0xc0, 0x00 })]
        [InlineData(0x00003fff, new byte[] { 0xff, 0x7f })]
        [InlineData(0x00004000, new byte[] { 0x81, 0x80, 0x00 })]
        [InlineData(0x001fffff, new byte[] { 0xff, 0xff, 0x7f })]
        [InlineData(0x00200000, new byte[] { 0x81, 0x80, 0x80, 0x00 })]
        [InlineData(0x08000000, new byte[] { 0xc0, 0x80, 0x80, 0x00 })]
        [InlineData(0x0fffffff, new byte[] { 0xff, 0xff, 0xff, 0x7f })]
        public void Writes_VLQ(int input, byte[] expected)
        {
            writer.WriteVLQ(input);

            stream.ToArray().Should().Equal(expected);
        }
    }
}
