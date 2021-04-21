using Jither.CommandLine;
using Jither.Logging;
using System;
using System.Collections.Generic;
using ImuseSequencer.Playback;
using System.IO;
using Jither.Imuse;
using Jither.Imuse.Files;

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
            SoundFile soundFile;
            try
            {
                soundFile = new SoundFile(options.InputPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                throw new ImuseSequencerException($"Cannot open file: {ex.Message}", ex);
            }

            var target = options.Target == SoundTarget.Unknown ? soundFile.Target : options.Target;
            if (target == SoundTarget.Unknown)
            {
                throw new ImuseSequencerException("Unable to determine target device. Please specify it as an argument.");
            }

            logger.Info($"Playing {options.InputPath}");

            try
            {
                using (var transmitter = new OutputDeviceTransmitter(options.DeviceId))
                {
                    using (var engine = new ImuseEngine(transmitter, target))
                    {
                        // Clean up, even with Ctrl+C
                        var cancelHandler = new ConsoleCancelEventHandler((sender, e) =>
                        {
                            logger.Warning("Abrupt exit - trying to clean up...");
                            engine.Dispose();
                            transmitter.Dispose();
                        });
                        Console.CancelKeyPress += cancelHandler;

                        engine.RegisterSound(0, soundFile);
                        engine.Play();

                        // TODO: Temporary quick-hack to play everything when engine is done.
                        transmitter.Send();

                        Console.ReadKey(intercept: true);

                        Console.CancelKeyPress -= cancelHandler;
                    }
                }
            }
            catch (ImuseException ex)
            {
                throw new ImuseSequencerException(ex.Message, ex);
            }
        }
    }
}
