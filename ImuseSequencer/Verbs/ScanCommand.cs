using Jither.CommandLine;
using Jither.Imuse.Files;
using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Files;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImuseSequencer.Verbs
{
    [Verb("scan", Help = "Scans MIDI files for iMUSE MIDI commands and lists them")]
    public class ScanOptions : CommonOptions
    {
        [Positional(0, Help = "Path to folder with MIDI files.", Name = "path", Required = true)]
        public string Path { get; set; }

        [Option('s', "skipped", Help = "Lists skipped files with info on why they were skipped.")]
        public bool ListSkipped { get; set; }
    }

    public class ScanCommand : Command
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ScanCommand));
        private readonly ScanOptions options;
        public ScanCommand(ScanOptions options) : base(options)
        {
            this.options = options;
        }

        public override void Execute()
        {
            var files = Directory.EnumerateFiles(options.Path, "*.*", new EnumerationOptions { MatchType = MatchType.Simple, RecurseSubdirectories = true });
            HashSet<string> imuseEvents = new HashSet<string>();
            foreach (var path in files)
            {
                string fileName = Path.GetRelativePath(options.Path, path);

                imuseEvents.Clear();
                SoundFile soundFile;
                try
                {
                    soundFile = new SoundFile(path);
                }
                catch (MidiFileException ex)
                {
                    if (options.ListSkipped)
                    {
                        logger.Warning($"{fileName}: {ex.Message}");
                    }
                    continue;
                }
                foreach (var track in soundFile.Midi.Tracks)
                {
                    foreach (var evt in track.Events)
                    {
                        if (evt.Message is ImuseMessage imuse)
                        {
                            imuseEvents.Add(imuse.ImuseMessageName);
                        }
                    }
                }

                if (imuseEvents.Count == 0)
                {
                    logger.Info($"{fileName} has no iMUSE events");
                }
                else
                {
                    logger.Info($"{fileName}: {String.Join(", ", imuseEvents)}");
                }
            }
        }
    }
}
