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
    public class PlayOptions : CommonOptions, ICustomParsing
    {
        [Positional(0, Name = "input file", Help = "Path to input MIDI file (Standard MIDI file or LEC chunk - SOUN, SOU, ADL, ROL etc.)", Required = true)]
        public string InputPath { get; set; }

        [Positional(1, Name = "output file", Help = "Path to output MIDI file. Cannot be combined with output to device.")]
        public string OutputPath { get; set; }

        [Option('o', "output", Help = "ID of MIDI output device. Cannot be combined with output to file.", ArgName = "device id")]
        public string DeviceId { get; set; }

        [Option('t', "target", Help = "Playback target device. 'Unknown' will determine from LEC chunk, if present.", ArgName = "target", Default = SoundTarget.Unknown)]
        public SoundTarget Target { get; set; }



        [Examples]
        public static IEnumerable<Example<PlayOptions>> Examples => new[]
        {
            new Example<PlayOptions>("Play file using MIDI output device 2", new PlayOptions { InputPath = "LARGO.rol", DeviceId = "2" }),
            new Example<PlayOptions>("Play file with MT-32 as target", new PlayOptions { InputPath = "OFFICE.mid", DeviceId = "2", Target = SoundTarget.Roland })
        };

        public bool ToFile => OutputPath != null;
        public bool ToDevice => DeviceId != null;

        public void AfterParse()
        {
            if (ToFile && ToDevice)
            {
                throw new CustomParserException("Cannot output to both device and MIDI file at the same time. Pick one.");
            }

            if (!ToFile && !ToDevice)
            {
                throw new CustomParserException("Please specify either an output MIDI file or an output MIDI device.");
            }
        }
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
                throw new ImuseSequencerException($"Cannot open input file: {ex.Message}", ex);
            }

            var target = options.Target == SoundTarget.Unknown ? soundFile.Target : options.Target;
            if (target == SoundTarget.Unknown)
            {
                throw new ImuseSequencerException("Unable to determine target device. Please specify it as an argument.");
            }

            if (options.ToDevice)
            {
                PlayToDevice(soundFile, target);
            }
            else
            {
                PlayToFile(soundFile, target);
            }
        }

        private ConsoleCancelEventHandler cancelHandler;

        private void SetupCancelHandler(ImuseEngine engine, ITransmitter transmitter)
        {
            // Clean up, even with Ctrl+C
            cancelHandler = new ConsoleCancelEventHandler((sender, e) =>
            {
                logger.Warning("Abrupt exit - trying to clean up...");
                engine.Dispose();
                if (transmitter is IDisposable disposableTransmitter)
                {
                    disposableTransmitter.Dispose();
                }
            });
            Console.CancelKeyPress += cancelHandler;
        }

        private void TearDownCancelHandler()
        {
            if (cancelHandler != null)
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }

        private IOutputTransmitter CreateTransmitter()
        {
            // TODO: Temporary debugging measure - s:<device-id> selects stream transmitter
            var idParts = options.DeviceId.Split(':');
            string strDeviceId;
            string transmitterType = null;
            if (idParts.Length == 2)
            {
                transmitterType = idParts[0];
                strDeviceId = idParts[1];
            }
            else
            {
                strDeviceId = idParts[0];
            }

            if (!Int32.TryParse(strDeviceId, out int deviceId))
            {
                throw new ImuseSequencerException($"Invalid device ID: {strDeviceId}");
            }

            switch (transmitterType)
            {
                case "s":
                    return new OutputStreamTransmitter(deviceId);
                default:
                    return new OutputDeviceTransmitter(deviceId);
            }
        }
        private void PlayToDevice(SoundFile soundFile, SoundTarget target)
        {
            logger.Info($"Playing <c#88cc55>{options.InputPath}</c>...");

            try
            {
                using (var transmitter = CreateTransmitter())
                {
                    using (var engine = new ImuseEngine(transmitter, target))
                    {
                        // Clean up, even with Ctrl+C
                        SetupCancelHandler(engine, transmitter);

                        engine.RegisterSound(0, soundFile);
                        engine.Play();

                        // TODO: Temporary quick-hack to play everything when engine is done.
                        transmitter.Send();

                        Console.ReadKey(intercept: true);

                        TearDownCancelHandler();
                    }
                }
            }
            catch (ImuseException ex)
            {
                throw new ImuseSequencerException(ex.Message, ex);
            }
        }

        private void PlayToFile(SoundFile soundFile, SoundTarget target)
        {
            logger.Info($"Writing playback of <c#88cc55>{options.InputPath}</c> to <c#88cc55>{options.OutputPath}</c>...");

            try
            {
                var transmitter = new MidiFileWriterTransmitter();
                using (var engine = new ImuseEngine(transmitter, target))
                {
                    // Clean up, even with Ctrl+C
                    SetupCancelHandler(engine, transmitter);

                    engine.RegisterSound(0, soundFile);
                    engine.Play();

                    // TODO: Temporary quick-hack to play everything when engine is done.
                    transmitter.Write(options.OutputPath);

                    TearDownCancelHandler();
                }
            }
            catch (ImuseException ex)
            {
                throw new ImuseSequencerException(ex.Message, ex);
            }
        }
    }
}
