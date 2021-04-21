using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jither.Midi.Parsing
{
    public class MidiFile
    {
        private readonly List<MidiTrack> tracks = new();
        private readonly MidiFileOptions options;

        private Timeline timeline;

        /// <summary>
        /// Standard MIDI file format (0, 1, 2).
        /// </summary>
        public int Format { get; private set; }

        /// <summary>
        /// Number of tracks.
        /// </summary>
        public int TrackCount { get; private set; }

        /// <summary>
        /// Type of division - PPQN or one of the SMPTE timecode frame rates - 24, 25, 29 (29.97) or 30.
        /// </summary>
        public DivisionType DivisionType { get; private set; }

        /// <summary>
        /// Ticks per quarter note in PPQN. 0 if not PPQN.
        /// </summary>
        public int TicksPerQuarterNote { get; private set; }

        /// <summary>
        /// Ticks per frame in SMPTE division. 0 if not SMPTE.
        /// </summary>
        public int TicksPerFrame { get; private set; }

        /// <summary>
        /// List of all tracks in the file.
        /// </summary>
        public IReadOnlyList<MidiTrack> Tracks => tracks;

        /// <summary>
        /// Timeline of time signature changes. Only available for <see cref="DivisionType.Ppqn" />.
        /// </summary>
        public Timeline Timeline => timeline ??= DivisionType == DivisionType.Ppqn ? new Timeline(this) : null;

        public MidiFile(MidiFileOptions options = null)
        {
            // Currently, options are only used for loading - and this constructor is intended for saving.
            // However, doesn't hurt (much) to create it.
            this.options = options ?? new MidiFileOptions();
        }

        public MidiFile(string path, MidiFileOptions options = null) : this(options)
        {
            Load(File.OpenRead(path));
        }

        public MidiFile(Stream stream, MidiFileOptions options = null) : this(options)
        {
            Load(stream);
        }

        public MidiFile(MidiReader reader, MidiFileOptions options = null) : this(options)
        {
            Load(reader);
        }

        private void Load(Stream stream)
        {
            using (var reader = new MidiReader(stream))
            {
                Load(reader);
            }
        }

        private void Load(MidiReader reader)
        {
            ReadHeaderChunk(reader);

            for (uint i = 0; i < TrackCount; i++)
            {
                tracks.Add(ReadTrackChunk(reader, i));
            }
        }

        private void ReadHeaderChunk(MidiReader reader)
        {
            var type = reader.ReadChunkType();
            if (type != "MThd")
            {
                throw new MidiFileException($"Not a valid MIDI file: Missing MThd chunk");
            }
            uint size = reader.ReadUint32();
            if (size != 6)
            {
                throw new MidiFileException($"Not a valid MIDI file: Size of MThd chunk isn't 6 bytes.");
            }
            Format = reader.ReadUint16();
            TrackCount = reader.ReadUint16();
            ushort division = reader.ReadUint16();
            if ((division & 0x8000) == 0)
            {
                DivisionType = DivisionType.Ppqn;
                TicksPerQuarterNote = division & 0x7fff;
            }
            else
            {
                sbyte smpteFormat = (sbyte)(division >> 8);
                DivisionType = smpteFormat switch
                {
                    -24 => DivisionType.Smpte24,
                    -25 => DivisionType.Smpte25,
                    -29 => DivisionType.Smpte29,
                    -30 => DivisionType.Smpte30,
                    _ => throw new MidiFileException($"Not a valid MIDI file: Unknown smpteFormat: {smpteFormat}")
                };
                TicksPerFrame = division & 0xff;
            }
        }

        private MidiTrack ReadTrackChunk(MidiReader reader, uint trackIndex)
        {
            string type = reader.ReadChunkType();
            if (type != "MTrk")
            {
                throw new MidiFileException($"Not a valid MIDI file: Didn't find expected MTrk chunk.");
            }
            uint size = reader.ReadUint32();
            
            // A MIDI file cannot actually exceed uint size (and isn't likely to need to in any event...)
            uint end = (uint)(reader.Position + size);

            var events = new List<MidiEvent>();

            long absoluteTicks = 0;
            int runningStatus = -1;

            while (reader.Position < end)
            {
                int deltaTicks = reader.ReadVLQ();

                byte status = reader.ReadStatus(runningStatus);

                // Doesn't hurt to get channel here, even if System Messages don't use it:
                int channel = status & 0x0f;
                byte command = (byte)(status & 0xf0);
                MidiMessage message = command switch
                {
                    0x80 => new NoteOffMessage(channel, key: reader.ReadByte(), velocity: reader.ReadByte()),
                    0x90 => new NoteOnMessage(channel, key: reader.ReadByte(), velocity: reader.ReadByte()),
                    0xa0 => new PolyPressureMessage(channel, key: reader.ReadByte(), pressure: reader.ReadByte()),
                    0xb0 => ControlChangeMessage.Create(channel, controller: reader.ReadByte(), value: reader.ReadByte()),
                    0xc0 => new ProgramChangeMessage(channel, program: reader.ReadByte()),
                    0xd0 => new ChannelPressureMessage(channel, pressure: reader.ReadByte()),
                    0xe0 => new PitchBendChangeMessage(channel, bender: (ushort)(reader.ReadByte() | reader.ReadByte() << 7)),
                    0xf0 => CreateSystemMessage(reader, status),
                    // This can never actually happen - ReadStatus will always return a status >= 0x80 or throw
                    _ => throw new MidiFileException($"Unexpected MIDI message command byte: {status}")
                };

                // Update running status. But: "Running Status will be stopped when any other Status byte [than channel/mode] intervenes."
                runningStatus = command != 0xf0 ? status : -1;

                absoluteTicks += deltaTicks;

                var evt = new MidiEvent(absoluteTicks, deltaTicks, message);
                events.Add(evt);
            }

            return new MidiTrack(trackIndex, events);
        }

        private MidiMessage CreateSystemMessage(MidiReader reader, byte status)
        {
            byte[] data;
            switch (status)
            {
                case 0xf0:
                    data = reader.ReadVariableBytes();
                    int manufacturerId = data[0];
                    if (data[0] == 0x00)
                    {
                        if (data.Length < 3)
                        {
                            throw new MidiFileException($"Expected 3 byte sysex manufacturer ID, but only {data.Length} bytes in sysex data.");
                        }
                        manufacturerId = data[1] << 7 | data[2];
                    }
                    if (options.SysexParsers.TryGetValue(manufacturerId, out var sysexParser))
                    {
                        return sysexParser.Parse(data);
                    }
                    return new SysexMessage(data, continuation: false);
                case 0xf7:
                    data = reader.ReadVariableBytes();
                    return new SysexMessage(data, continuation: true);
                case 0xff:
                    byte type = reader.ReadByte();
                    data = reader.ReadVariableBytes();
                    return MetaMessage.Create(type, data);
                default:
                    throw new NotSupportedException($"Unsupported system message with status byte {status}");
            }
        }

        public override string ToString()
        {
            string result = $"Format {Format}, Tracks: {TrackCount}, Division: {DivisionType} - ";
            result += DivisionType switch
            {
                DivisionType.Smpte24 => TicksPerFrame,
                DivisionType.Smpte25 => TicksPerFrame,
                DivisionType.Smpte29 => TicksPerFrame,
                DivisionType.Smpte30 => TicksPerFrame,
                DivisionType.Ppqn => TicksPerQuarterNote,
                _ => "unknown division type"
            };

            return result;
        }
    }
}
