using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jither.Midi.Files
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
        public int TrackCount => tracks.Count;

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

        public MidiFile(int format, DivisionType divisionType, int ticksPerDivision)
        {
            Format = format;
            DivisionType = divisionType;
            switch (DivisionType)
            {
                case DivisionType.Ppqn:
                    TicksPerQuarterNote = ticksPerDivision;
                    break;
                default:
                    TicksPerFrame = ticksPerDivision;
                    break;
            }
        }

        public MidiTrack AddTrack(List<MidiEvent> events)
        {
            var track = new MidiTrack(tracks.Count, events);
            tracks.Add(track);
            return track;
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
            int trackCount = ReadHeaderChunk(reader);

            for (int i = 0; i < trackCount; i++)
            {
                tracks.Add(ReadTrackChunk(reader, i));
            }
        }

        public void Save(string path)
        {
            using (var stream = File.Open(path, FileMode.Create))
            {
                Save(stream);
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new MidiFileWriter(stream))
            {
                Save(writer);
            }
        }

        public void Save(MidiFileWriter writer)
        {
            WriteHeaderChunk(writer);

            foreach (var track in Tracks)
            {
                WriteTrackChunk(writer, track);
            }
        }

        private int ReadHeaderChunk(MidiReader reader)
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
            int trackCount = reader.ReadUint16();
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

            return trackCount;
        }

        private void WriteHeaderChunk(MidiFileWriter writer)
        {
            writer.WriteChunkType("MThd");
            writer.WriteUint32(6); // chunk size
            writer.WriteUint16((ushort)Format);
            writer.WriteUint16((ushort)TrackCount);

            ushort division;
            switch (DivisionType)
            {
                case DivisionType.Ppqn:
                    division = (ushort)TicksPerQuarterNote;
                    break;
                case DivisionType.Smpte24:
                case DivisionType.Smpte25:
                case DivisionType.Smpte29:
                case DivisionType.Smpte30:
                    division = (ushort)(-(ushort)DivisionType << 8 | TicksPerFrame);
                    break;
                default:
                    throw new MidiFileException($"Unsupported division type: {DivisionType}");
            }
            writer.WriteUint16(division);
        }

        private MidiTrack ReadTrackChunk(MidiReader reader, int trackIndex)
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
                    _ => throw new MidiFileException($"Unexpected MIDI message command byte: 0x{status:x2} at reader position {reader.Position}")
                };

                // Update running status. But: "Running Status will be stopped when any other Status byte [than channel/mode] intervenes."
                runningStatus = command != 0xf0 ? status : -1;

                absoluteTicks += deltaTicks;

                var evt = new MidiEvent(absoluteTicks, message);
                events.Add(evt);
            }

            return new MidiTrack(trackIndex, events);
        }

        public void WriteTrackChunk(MidiFileWriter writer, MidiTrack track)
        {
            writer.WriteChunkType("MTrk");
            long sizePosition = writer.Position;
            writer.WriteUint32(0); // Temporary, until we know the size

            long previousTicks = 0;

            foreach (var evt in track.Events)
            {
                long deltaTime = evt.AbsoluteTicks - previousTicks;
                writer.WriteVLQ((int)deltaTime);

                evt.Message.Write(writer);

                previousTicks = evt.AbsoluteTicks;
            }

            // Write chunk size:
            long endPosition = writer.Position;
            writer.Position = sizePosition;
            writer.WriteUint32((uint)(endPosition - sizePosition - 4));
            writer.Position = endPosition;
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
                    throw new NotSupportedException($"Unsupported system message with status byte 0x{status:x2}");
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
