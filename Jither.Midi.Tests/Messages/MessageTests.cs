using Jither.Midi.Files;
using System;
using System.IO;
using Xunit;
using FluentAssertions;

namespace Jither.Midi.Messages
{
    public class MessageTests : IDisposable
    {
        private MemoryStream stream;
        private MidiFileWriter writer;

        public MessageTests()
        {
            stream = new MemoryStream();
            writer = new MidiFileWriter(stream);
        }

        public void Dispose()
        {
            writer.Dispose();
            stream.Dispose();
        }

        [Fact]
        public void NoteOffMessage_writes_correctly_to_file()
        {
            var message = new NoteOffMessage(10, 64, 127);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0x8a, 0x40, 0x7f });
        }

        [Fact]
        public void NoteOnMessage_writes_correctly_to_file()
        {
            var message = new NoteOnMessage(10, 64, 127);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0x9a, 0x40, 0x7f });
        }

        [Fact]
        public void PolyPressureMessage_writes_correctly_to_file()
        {
            var message = new PolyPressureMessage(15, 32, 32);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xaf, 0x20, 0x20 });
        }

        [Fact]
        public void ControlChangeMessage_writes_correctly_to_file()
        {
            var message = ControlChangeMessage.Create(9, MidiController.Pan, 127);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xb9, 0x0a, 127 });
        }

        [Theory]
        [InlineData(MidiController.AllSoundOff)]
        [InlineData(MidiController.LocalControl)]
        [InlineData(MidiController.AllNotesOff)]
        [InlineData(MidiController.ResetAll)]
        [InlineData(MidiController.OmniOn)]
        [InlineData(MidiController.OmniOff)]
        [InlineData(MidiController.PolyOn)]
        [InlineData(MidiController.PolyOff)]
        public void ChannelModeMessages_write_correctly_to_file(MidiController controller)
        {
            var message = ControlChangeMessage.Create(8, controller, 127);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xb8, (byte)controller, 127 });
        }

        [Fact]
        public void ProgramChangeMessage_writes_correctly_to_file()
        {
            var message = new ProgramChangeMessage(7, 92);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xc7, 92 });
        }

        [Fact]
        public void ChannelPressureMessage_writes_correctly_to_file()
        {
            var message = new ChannelPressureMessage(9, 92);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xd9, 92 });
        }

        [Theory]
        [InlineData(0, 0x00, 0x00)]
        [InlineData(127, 0x00, 0x7f)]
        [InlineData(128, 0x01, 0x00)]
        [InlineData(4095, 0x1f, 0x7f)]
        [InlineData(4096, 0x20, 0x00)]
        [InlineData(8191, 0x3f, 0x7f)]
        public void PitchBendChangeMessage_writes_correctly_to_file(ushort value, byte expectedValue1, byte expectedValue2)
        {
            var message = new PitchBendChangeMessage(4, value);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xe4, expectedValue1, expectedValue2 });
        }

        [Fact]
        public void SysexMessage_writes_correctly_to_file()
        {
            var message = new SysexMessage(new byte[] { 0x7d, 0x30, 0x20, 0x10, 0x7f });
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xf0, 0x05, 0x7d, 0x30, 0x20, 0x10, 0x7f });
        }

        [Fact]
        public void SysexMessage_continued_writes_correctly_to_file()
        {
            var message = new SysexMessage(new byte[] { 0x7d, 0x30, 0x20, 0x10 }); // No EOX (F7)
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xf0, 0x04, 0x7d, 0x30, 0x20, 0x10 });
        }

        [Fact]
        public void SysexMessage_continuation_writes_correctly_to_file()
        {
            var message = new SysexMessage(new byte[] { 0x7d, 0x30, 0x20, 0x10, 0x7f }, continuation: true);
            message.Write(writer);
            stream.ToArray().Should().Equal(new byte[] { 0xf7, 0x05, 0x7d, 0x30, 0x20, 0x10, 0x7f });
        }

        [Fact]
        public void SequenceNumberMessage_writes_correctly_to_file()
        {
            var message = new SequenceNumberMessage(256);
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x00, 0x02, 0x01, 0x00 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void TextEventMessage_writes_correctly_to_file()
        {
            var message = new TextEventMessage("test");
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x01, 0x04, 0x74, 0x65, 0x73, 0x74 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void CopyrightNoticeMessage_writes_correctly_to_file()
        {
            var message = new CopyrightNoticeMessage("test");
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x02, 0x04, 0x74, 0x65, 0x73, 0x74 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void SequenceNameMessage_writes_correctly_to_file()
        {
            var message = new SequenceNameMessage("test");
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x03, 0x04, 0x74, 0x65, 0x73, 0x74 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void InstrumentNameMessage_writes_correctly_to_file()
        {
            var message = new InstrumentNameMessage("Oboe");
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x04, 0x04, 0x4f, 0x62, 0x6f, 0x65 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void LyricMessage_writes_correctly_to_file()
        {
            var message = new LyricMessage("lalala");
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x05, 0x06, 0x6c, 0x61, 0x6c, 0x61, 0x6c, 0x61 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void MarkerMessage_writes_correctly_to_file()
        {
            var message = new MarkerMessage("marker");
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x06, 0x06, 0x6d, 0x61, 0x72, 0x6b, 0x65, 0x72 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void CuePointMessage_writes_correctly_to_file()
        {
            var message = new CuePointMessage("Go!");
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x07, 0x03, 0x47, 0x6f, 0x21 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void MidiChannelPrefixMessage_writes_correctly_to_file()
        {
            var message = new MidiChannelPrefixMessage(15);
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x20, 0x01, 0x0f };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void EndOfTrackMessage_writes_correctly_to_file()
        {
            var message = new EndOfTrackMessage();
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x2f, 0x00 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void SetTempoMessage_writes_correctly_to_file()
        {
            var message = new SetTempoMessage(500000);
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x51, 0x03, 0x07, 0xa1, 0x20 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void SmpteOffsetMessage_writes_correctly_to_file()
        {
            var message = new SmpteOffsetMessage(20, 45, 30, 10, 99);
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x54, 0x05, 0x14, 0x2d, 0x1e, 0x0a, 0x63 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void TimeSignatureMessage_writes_correctly_to_file()
        {
            var message = new TimeSignatureMessage(12, 8, 36, 8);
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x58, 0x04, 0x0c, 0x08, 0x24, 0x08 };
            stream.ToArray().Should().Equal(expectedOutput);
        }


        [Fact]
        public void KeySignatureMessage_writes_correctly_to_file()
        {
            var message = new KeySignatureMessage(-4, minor: true);
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x59, 0x02, 0xfc, 0x01 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Fact]
        public void SequencerSpecificMessage_writes_correctly_to_file()
        {
            var message = new SequencerSpecificMessage(new byte[] { 0x7d, 0x01 });
            message.Write(writer);
            var expectedOutput = new byte[] { 0xff, 0x7f, 0x02, 0x7d, 0x01 };
            stream.ToArray().Should().Equal(expectedOutput);
        }

        [Theory]
        [InlineData(MidiController.AllSoundOff, typeof(AllSoundOffMessage))]
        [InlineData(MidiController.LocalControl, typeof(LocalControlMessage))]
        [InlineData(MidiController.AllNotesOff, typeof(AllNotesOffMessage))]
        [InlineData(MidiController.ResetAll, typeof(ResetAllMessage))]
        [InlineData(MidiController.OmniOn, typeof(OmniOnMessage))]
        [InlineData(MidiController.OmniOff, typeof(OmniOffMessage))]
        [InlineData(MidiController.PolyOn, typeof(PolyOnMessage))]
        [InlineData(MidiController.PolyOff, typeof(PolyOffMessage))]
        public void ChannelModeMessages_have_the_proper_type(MidiController controller, Type type)
        {
            var message = ControlChangeMessage.Create(8, controller, 127);
            message.Should().BeOfType(type);
        }
    }
}
