using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jither.Midi.Parsing
{
    public class MidiFileOptions
    {
        public bool ParseImuse { get; set; }
    }

    public class MidiFile
    {
        private readonly List<MidiTrack> tracks = new();
        private readonly MidiFileOptions options;

        private static readonly Dictionary<string, SoundTarget> targetsByChunk = new()
        {
            ["ADL "] = SoundTarget.Adlib,
            ["ROL "] = SoundTarget.Roland,
            ["SBL "] = SoundTarget.SoundBlaster,
            ["GMD "] = SoundTarget.GeneralMidi,
            ["MIDI"] = SoundTarget.GeneralMidi,
            ["TAN "] = SoundTarget.Tandy,
            ["SPK "] = SoundTarget.Speaker
        };

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
        /// Target device. Only available if MIDI was wrapped in target chunk.
        /// </summary>
        public SoundTarget Target { get; private set; }

        /// <summary>
        /// iMUSE MIDI header. Only available if MIDI includes MDhd chunk.
        /// </summary>
        public ImuseMidiHeader ImuseHeader { get; private set; }

        /// <summary>
        /// List of all tracks in the file.
        /// </summary>
        public IReadOnlyList<MidiTrack> Tracks => tracks;

        /// <summary>
        /// Timeline of time signature changes. Only available for <see cref="DivisionType.Ppqn" />.
        /// </summary>
        public Timeline Timeline { get; }

        public MidiFile(string path, MidiFileOptions options = null) : this(File.OpenRead(path), options)
        {

        }

        public MidiFile(Stream stream, MidiFileOptions options = null)
        {
            this.options = options ?? new MidiFileOptions();

            using (var reader = new MidiReader(stream))
            {
                ReadHeaderChunk(reader);

                for (uint i = 0; i < TrackCount; i++)
                {
                    tracks.Add(ReadTrackChunk(reader, i));
                }
            }

            if (DivisionType == DivisionType.Ppqn)
            {
                Timeline = new Timeline(this);
            }
        }

        private void ReadHeaderChunk(MidiReader reader)
        {
            string type = FindHeader(reader);
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

        private string FindHeader(MidiReader reader)
        {
            // Skip/check LEC specific chunks

            long chunkPosition = reader.Position;
            string type = reader.ReadChunkType();

            while (type != "MThd")
            {
                switch (type)
                {
                    case "SOUN":
                        // Typically outermost chunk in rooms.
                        // May have a nested SOU_ chunk (e.g. MI2) or immediate target chunks (e.g. SNM)
                        uint sounSize = reader.ReadUint32();
                        if (chunkPosition + sounSize != reader.Length)
                        {
                            throw new MidiFileException($"Not a valid SOUN MIDI file: Incorrect SOUN chunk size.");
                        }
                        break;
                    case "SOU ":
                        uint souSize = reader.ReadUint32();
                        if (chunkPosition + souSize + 8 != reader.Length)
                        {
                            throw new MidiFileException($"Not a valid SOU MIDI file: Incorrect SOU chunk size");
                        }
                        break;
                    case "MDhd":
                        // Found in most (all?) target chunks - variable length (e.g. 0 in DOTT)
                        uint mdhdSize = reader.ReadUint32();
                        // Just skip size for now
                        ImuseHeader = new ImuseMidiHeader(reader, mdhdSize);
                        break;
                    case "MDpg":
                        // Found in SNM and DOTT - variable length, not in all target chunks
                        uint mdpgSize = reader.ReadUint32();
                        // Just skip size for now
                        reader.Position += mdpgSize;
                        break;
                    default:
                        if (!targetsByChunk.TryGetValue(type, out var target))
                        {
                            throw new MidiFileException($"Not a valid MIDI file: Unknown header chunk type: {type}");
                        }
                        _ = reader.ReadUint32();
                        Target = target;
                        break;
                }
                chunkPosition = reader.Position;
                type = reader.ReadChunkType();
            }
            return type;
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
                    0xa0 => new PolyPressure(channel, key: reader.ReadByte(), pressure: reader.ReadByte()),
                    0xb0 => ControlChangeMessage.Create(channel, controller: reader.ReadByte(), value: reader.ReadByte()),
                    0xc0 => new ProgramChangeMessage(channel, program: reader.ReadByte()),
                    0xd0 => new ChannelPressureMessage(channel, pressure: reader.ReadByte()),
                    0xe0 => new PitchBendChangeMessage(channel, bender: (ushort)(reader.ReadByte() << 7 | reader.ReadByte())),
                    0xf0 => CreateSystemMessage(reader, status),
                    // This can never actually happen - ReadStatus will always return a status >= 0x80 or throw
                    _ => throw new MidiFileException($"Unexpected MIDI message command byte: {status}")
                };

                // Update running status. But: "Running Status will be stopped when any other Status byte [than channel/mode] intervenes."
                runningStatus = command != 0xf0 ? status : -1;

                // Although we type deltaTime as signed integer for convenience, it's never negative, so this cast is fine
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
                    if (options.ParseImuse && data[0] == 0x7d)
                    {
                        return ImuseMessage.Create(data);
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
            string result = $"Target {Target}, Format {Format}, Tracks: {TrackCount}, Division: {DivisionType} - ";
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
