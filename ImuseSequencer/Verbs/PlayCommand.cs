using ImuseSequencer.Helpers;
using Jither.CommandLine;
using Jither.Logging;
using Jither.Utilities;
using Jither.Midi.Devices;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Messages;
using Jither.Midi.Parsing;
using Jither.Midi.Sequencing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImuseSequencer.Playback;
using System.IO;
using ImuseSequencer.Messages;

namespace ImuseSequencer.Verbs
{
    [Verb("play", Help = "Plays file")]
    public class PlayOptions : CommonOptions
    {
        [Positional(0, Name = "input file", Help = "Input MIDI file (Standard MIDI file or LEC chunk - SOUN, SOU, ADL, ROL etc.)", Required = true)]
        public string InputPath { get; set; }

        [Option('o', "output", Help = "ID of MIDI output device.", ArgName = "device id", Required = true)]
        public int DeviceId { get; set; }

        [Option('t', "target", Help = "Playback target device. 'Unknown' will determine from LEC chunk, if present.", ArgName = "target", Default = SoundTarget.Unknown)]
        public SoundTarget Target { get; set; }

        [Examples]
        public static IEnumerable<Example<PlayOptions>> Examples => new[]
        {
            new Example<PlayOptions>("Play file using MIDI output device 2", new PlayOptions { InputPath = "LARGO.rol", DeviceId = 2 }),
            new Example<PlayOptions>("Play file with MT-32 as target", new PlayOptions { InputPath = "OFFICE.mid", DeviceId = 2, Target = SoundTarget.Roland })
        };
    }

    public class PlayCommand : Command
    {
        private readonly Logger logger = LogProvider.Get(nameof(PlayCommand));

        private readonly PlayOptions options;

        public PlayCommand(PlayOptions options) : base(options)
        {
            this.options = options;
        }

        public override void Execute()
        {
            MidiFile midiFile;
            try
            {
                midiFile = new MidiFile(options.InputPath, new MidiFileOptions().WithParser(new ImuseSysexParser()));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                throw new ImuseSequencerException($"Cannot open file: {ex.Message}", ex);
            }

            var target = options.Target == SoundTarget.Unknown ? midiFile.Target : options.Target;
            if (target == SoundTarget.Unknown)
            {
                throw new ImuseSequencerException("Unable to determine target device. Please specify it as an argument.");
            }

            logger.Info($"Playing {options.InputPath}");

            using (var engine = new ImuseEngine(options.DeviceId, target))
            {
                // Clean up, even with Ctrl+C
                var cancelHandler = new ConsoleCancelEventHandler((sender, e) =>
                {
                    logger.Warning("Abrupt exit - trying to clean up...");
                    engine.Dispose();
                });
                Console.CancelKeyPress += cancelHandler;

                engine.RegisterSound(0, midiFile);
                engine.Play();
                Console.ReadKey(intercept: true);
                Console.CancelKeyPress -= cancelHandler;
            }
        }
    }
}
