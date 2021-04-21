using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Parsing;
using Jither.Utilities;
using System.Collections.Generic;
using System.IO;

namespace Jither.Imuse.Files
{
    public class SoundFile
    {
        private static readonly Logger logger = LogProvider.Get(nameof(SoundFile));
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

        public MidiFile Midi { get; private set; }

        public SoundFile(string path)
        {
            Name = path;
            using (var stream = File.OpenRead(path))
            {
                using (var reader = new MidiReader(stream))
                {
                    // We allow (and in some cases, prefer) various LEC wrappers and headers before the MIDI itself:
                    FindMidiHeader(reader);
                    // Now continue reading the actual MIDI:
                    Midi = new MidiFile(reader, new MidiFileOptions().WithParser(new ImuseSysexParser()));
                }
            }
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
                    case "MDhd":
                        // Found in most (all?) target chunks - variable length (0 in DOTT)
                        // Contains various properties for iMUSE (e.g. sound priority etc.)
                        uint mdhdSize = reader.ReadUint32();
                        try
                        {
                            ImuseHeader = new ImuseMidiHeader(reader, mdhdSize);
                        }
                        catch (ImuseMidiHeaderException ex)
                        {
                            logger.Warning(ex.Message);
                        }
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
            // Rewind to start of MThd
            reader.Position -= 4;
        }

        public override string ToString()
        {
            return $"Target: {Target.GetFriendlyName()}";
        }
    }
}
