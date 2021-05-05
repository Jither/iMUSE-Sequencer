using Jither.CommandLine;
using Jither.Imuse.Files;
using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Files;
using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImuseSequencer.Verbs
{
    public enum PropertyType
    {
        ImuseMessage,
        CtrlMessage,
        SysMessage,
        ImuseHookId,
        ImuseUnknown
    }

    [Verb("scan", Help = "Scans MIDI files for various MIDI properties and lists them for each file")]
    public class ScanOptions : CommonOptions
    {
        [Positional(0, Help = "Path to folder with MIDI files.", Name = "path", Required = true)]
        public string Path { get; set; }

        [Option('t', "type", Help = "Type of property to scan for.", ArgName = "message type", Required = true)]
        public PropertyType Type { get; set; }

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
            HashSet<string> events = new();
            foreach (var path in files)
            {
                string fileName = Path.GetRelativePath(options.Path, path);

                events.Clear();
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

                EnumerateEvents(soundFile, events);

                if (events.Count == 0)
                {
                    logger.Info($"{fileName} has no events of this type.");
                }
                else
                {
                    if (options.Type == PropertyType.ImuseUnknown)
                    {
                        logger.Info($"{fileName}:");
                        foreach (var evt in events)
                        {
                            logger.Info($"  {evt}");
                        }
                    }
                    else
                    {
                        logger.Info($"{fileName}: {String.Join(", ", events)}");
                    }
                }
            }
        }

        private void EnumerateEvents(SoundFile soundFile, HashSet<string> events)
        {
            foreach (var track in soundFile.Midi.Tracks)
            {
                foreach (var evt in track.Events)
                {
                    switch (options.Type)
                    {
                        case PropertyType.ImuseMessage:
                            if (evt.Message is ImuseMessage imuse)
                            {
                                events.Add(imuse.ImuseMessageName);
                            }
                            break;
                        case PropertyType.CtrlMessage:
                            if (evt.Message is ControlChangeMessage ctrl)
                            {
                                events.Add(MidiHelper.GetControllerName(ctrl.Controller));
                            }
                            break;
                        case PropertyType.SysMessage:
                            if (evt.Message is not ChannelMessage)
                            {
                                if (evt.Message is MetaMessage meta)
                                {
                                    events.Add(meta.TypeName);
                                }
                                else
                                {
                                    events.Add(evt.Message.Name);
                                }
                            }
                            break;
                        case PropertyType.ImuseHookId:
                            if (evt.Message is ImuseHook hook)
                            {
                                events.Add($"0x{hook.Hook:x2}");
                            }
                            break;
                        case PropertyType.ImuseUnknown:
                            if (evt.Message is ImuseUnknown unknown)
                            {
                                events.Add(unknown.Data.ToHex());
                            }
                            break;
                    }
                }
            }
        }
    }
}
