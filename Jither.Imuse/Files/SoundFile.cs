using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Files;
using Jither.Midi.Messages;
using Jither.Utilities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Jither.Imuse.Files
{
    public class Position
    {
        public int TrackIndex { get; }
        public int Beat { get; }
        public int Tick { get; }

        public Position(int trackIndex, BeatPosition beatPosition) : this(trackIndex, beatPosition.TotalBeat, beatPosition.Tick)
        {
        }

        public Position(int trackIndex, int beat, int tick)
        {
            TrackIndex = trackIndex;
            Beat = beat;
            Tick = tick;
        }

        public override string ToString()
        {
            return $"{TrackIndex}b{Beat + 1}.{Tick}";
        }
    }

    public class HookInfo
    {
        private readonly List<Position> positions = new();

        public HookType Type { get; }
        public int Id { get; }
        public int Channel { get; }
        public IEnumerable<Position> Positions => positions;

        public HookInfo(HookType type, int id, int channel)
        {
            Type = type;
            Id = id;
            Channel = channel;
        }

        public void AddPosition(int trackIndex, BeatPosition beatPosition)
        {
            positions.Add(new Position(trackIndex, beatPosition));
        }

        public override string ToString()
        {
            string result = $"set-{Type.GetDisplayName()}-hook {Id}";
            if (Type != HookType.Jump && Type != HookType.Transpose)
            {
                result += $" {Channel}";
            }
            return result;
        }
    }

    public class MarkerInfo
    {
        public int Id { get; }
        public Position Position { get; }

        public MarkerInfo(int trackIndex, BeatPosition beatPosition, int id)
        {
            Position = new Position(trackIndex, beatPosition);
            Id = id;
        }

        public override string ToString()
        {
            return $"  {Id} @ {Position}";
        }
    }

    public class SetLoopInfo
    {
        public int Count { get; }
        public Position StartPosition { get; }
        public Position EndPosition { get; }

        public SetLoopInfo(int trackIndex, ImuseSetLoop setLoop)
        {
            Count = setLoop.Count;
            StartPosition = new Position(trackIndex, setLoop.StartBeat, setLoop.StartTick);
            EndPosition = new Position(trackIndex, setLoop.EndBeat, setLoop.EndTick);
        }

        public override string ToString()
        {
            return $"{StartPosition} - {EndPosition}  {Count} times";
        }
    }

    public class ClearLoopInfo
    {
        public Position Position { get; }
        public ClearLoopInfo(int trackIndex, BeatPosition position)
        {
            Position = new Position(trackIndex, position);
        }

        public override string ToString()
        {
            return $"{Position}";
        }
    }

    public class InteractivityInfo
    {
        private readonly List<HookInfo> hooks = new();
        private readonly List<ClearLoopInfo> clearLoops = new();
        private readonly List<SetLoopInfo> setLoops = new();
        private readonly List<MarkerInfo> markers = new();

        public IEnumerable<HookInfo> Hooks => hooks;
        public IEnumerable<SetLoopInfo> SetLoops => setLoops;
        public IEnumerable<ClearLoopInfo> ClearLoops => clearLoops;
        public IEnumerable<MarkerInfo> Markers => markers;

        public InteractivityInfo()
        {

        }

        public void AddMessage(int trackIndex, BeatPosition position, ImuseMessage message)
        {
            switch (message)
            {
                case ImuseClearLoop:
                    clearLoops.Add(new ClearLoopInfo(trackIndex, position));
                    break;
                case ImuseSetLoop setLoop:
                    setLoops.Add(new SetLoopInfo(trackIndex, setLoop));
                    break;
                case ImuseHook hookMessage:
                    var hook = hooks.Find(h => h.Type == hookMessage.Type && h.Id == hookMessage.Hook && h.Channel == hookMessage.Channel);
                    if (hook == null)
                    {
                        hook = new HookInfo(hookMessage.Type, hookMessage.Hook, hookMessage.Channel);
                        hooks.Add(hook);
                    }
                    hook.AddPosition(trackIndex, position);
                    break;
                case ImuseMarker marker:
                    markers.Add(new MarkerInfo(trackIndex, position, marker.Id));
                    break;
                case ImuseV3Marker marker3:
                    markers.Add(new MarkerInfo(trackIndex, position, marker3.Id));
                    break;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (setLoops.Count > 0)
            {
                builder.AppendLine("Loops:");
                foreach (var loop in setLoops)
                {
                    builder.AppendLine($"  {loop}");
                }
            }

            if (clearLoops.Count > 0)
            {
                builder.AppendLine("Clear-loops:");
                foreach (var clearLoop in clearLoops)
                {
                    builder.AppendLine($"  {clearLoop}");
                }
            }

            if (hooks.Count > 0)
            {
                builder.AppendLine("Hooks:");
                foreach (var hook in hooks)
                {
                    builder.AppendLine($"  {hook}");
                }
            }

            if (markers.Count > 0)
            {
                builder.AppendLine("Markers:");
                foreach (var marker in markers)
                {
                    builder.AppendLine($"  {marker}");
                }
            }

            return builder.ToString();
        }
    }

    public enum ImuseVersion
    {
        [Display(ShortName = "unknown")]
        Unknown,
        [Display(ShortName = "v1", Name = "v1 (MI2, FOA)")]
        V1,
        [Display(ShortName = "v2", Name = "v2 (DOTT, X-Wing, Tie-Fighter)")]
        V2,
        [Display(ShortName = "v3", Name = "v3 (SNM, Dark Forces)")]
        V3
    }

    public class SoundFile
    {
        private static readonly Logger logger = LogProvider.Get(nameof(SoundFile));
        private static readonly Dictionary<string, SoundTarget> targetsByChunk = new()
        {
            ["ADL "] = SoundTarget.Adlib,
            ["ROL "] = SoundTarget.Roland,
            ["GMD "] = SoundTarget.GeneralMidi,
            ["MIDI"] = SoundTarget.GeneralMidi,
            ["SPK "] = SoundTarget.Speaker
        };

        /// <summary>
        /// Name of the sound file.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Target device. Only available if MIDI was wrapped in target chunk.
        /// </summary>
        public SoundTarget Target { get; private set; }

        /// <summary>
        /// iMUSE MIDI header. Only available if sound file includes MDhd chunk.
        /// </summary>
        public ImuseMidiHeader ImuseHeader { get; private set; }

        public byte[] PgHeader { get; private set; }

        public MidiFile Midi { get; private set; }

        public ImuseVersion ImuseVersion { get; private set; }

        protected SoundFile(MidiReader reader)
        {
            // We allow (and in some cases, prefer) various LEC wrappers and headers before the MIDI itself:
            FindMidiHeader(reader);
            // Now continue reading the actual MIDI:
            Midi = new MidiFile(reader, new MidiFileOptions().WithParser(new ImuseSysexParser()));
            if (Midi.Timeline == null)
            {
                throw new MidiFileException($"iMUSE does not support non-PPQN files");
            }
            Midi.Timeline.ApplyBeatPositions();

            ImuseVersion = DetermineVersion();
        }

        public static SoundFile Load(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var result = Load(stream);
                result.Name = path;
                return result;
            }
        }

        public static SoundFile Load(Stream stream)
        {
            using (var reader = new MidiReader(stream))
            {
                return Load(reader);
            }
        }

        public static SoundFile Load(MidiReader reader)
        {
            return new SoundFile(reader);
        }

        private ImuseVersion DetermineVersion()
        {
            bool imuseMessageFound = false;
            foreach (var track in Midi.Tracks)
            {
                foreach (var evt in track.Events)
                {
                    if (evt.Message is not ImuseMessage message)
                    {
                        continue;
                    }

                    imuseMessageFound = true;

                    switch (evt.Message)
                    {
                        case ImuseAllocPart:
                        case ImuseDeallocPart:
                        case ImuseDeallocAllParts:
                            return ImuseVersion.V1;
                        case ImuseV3HookJump:
                        case ImuseV3Marker:
                        case ImuseV3Text:
                            return ImuseVersion.V3;
                    }
                }
            }
            // If we did find iMUSE messages, but not V3 or V1, then it must be V2.
            // If we didn't find any, we can't possibly know what version the file is.
            return imuseMessageFound ? ImuseVersion.V2 : ImuseVersion.Unknown;
        }

        private void FindMidiHeader(MidiReader reader)
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
                    case "AUhd":
                        // This is in SBL files - which aren't MIDIs but audio data
                        throw new MidiFileException($"Not a valid MIDI file: Looks like SBL audio.");
                    case "MDhd":
                        // Found in most (all?) target chunks - variable length (0 in DOTT)
                        // Contains various properties for iMUSE (e.g. sound priority etc.)
                        uint mdhdSize = reader.ReadUint32();
                        // Size of 0 is expected - just skip it.
                        if (mdhdSize > 0)
                        {
                            try
                            {
                                ImuseHeader = new ImuseMidiHeader(reader, mdhdSize);
                            }
                            catch (ImuseMidiHeaderException ex)
                            {
                                logger.Warning(ex.Message);
                            }
                        }
                        break;
                    case "MDpg":
                        // Found in SNM and DOTT - variable length, not in all target chunks.
                        // It lists what appears to be the initial patches/programs in the MIDI.
                        // It doesn't appear to be used at playback, though.
                        uint mdpgSize = reader.ReadUint32();
                        PgHeader = new byte[mdpgSize];
                        reader.Read(PgHeader, 0, (int)mdpgSize);
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
            // Rewind to start of MThd
            reader.Position -= 4;
        }

        public InteractivityInfo GetInteractivityInfo()
        {
            var info = new InteractivityInfo();
            for (int trackIndex = 0; trackIndex < Midi.TrackCount; trackIndex++)
            {
                var track = Midi.Tracks[trackIndex];
                foreach (var evt in track.Events)
                {
                    if (evt.Message is ImuseMessage imuse)
                    {
                        info.AddMessage(trackIndex, evt.BeatPosition, imuse);
                    }
                }
            }

            return info;
        }

        public override string ToString()
        {
            return $"Target: {Target.GetDisplayName()}";
        }
    }
}
