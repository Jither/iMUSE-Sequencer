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

        [Option("loop-limit", Help = "Limits the number of loops performed by the sequencer. This is useful (necessary) for e.g. non-interactive recording. 0 indicates no limit for device outputs and 3 for MIDI file output.", ArgName = "limit", Default = 0)]
        public int LoopLimit { get; set; }

        [Option("jump-limit", Help = "Limits the number of times each jump hook is performed by the sequencer. This is useful (necessary) for e.g. non-interactive recording. 0 indicates no limit for device outputs and 3 for MIDI file output.", ArgName = "limit", Default = 0)]
        public int JumpLimit { get; set; }

        [Option("latency", Help = "Latency (in ticks) of playback. Only has an effect on device output. You probably shouldn't set this to something silly like 1...", ArgName = "ticks", Default = 480)]
        public int Latency { get; set; }

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

            if (JumpLimit == 0)
            {
                JumpLimit = ToFile ? 3 : int.MaxValue;
            }

            if (LoopLimit == 0)
            {
                LoopLimit = ToFile ? 3 : int.MaxValue;
            }

            if (Latency < 1)
            {
                throw new CustomParserException("Don't set latency to a silly value...");
            }
        }
    }

    public class PlayCommand : Command
    {
        private readonly Logger logger = LogProvider.Get(nameof(PlayCommand));

        private readonly PlayOptions options;
        private readonly ImuseOptions imuseOptions;

        public PlayCommand(PlayOptions options) : base(options)
        {
            this.options = options;
            imuseOptions = new ImuseOptions { JumpLimit = options.JumpLimit, LoopLimit = options.LoopLimit };
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

        private ITransmitter CreateTransmitter()
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

            return transmitterType switch
            {
                "s" => new OutputStreamTransmitter(deviceId),
                _ => new OutputDeviceTransmitter(deviceId, options.Latency),
            };
        }
        private void PlayToDevice(SoundFile soundFile, SoundTarget target)
        {
            logger.Info($"Playing <c#88cc55>{options.InputPath}</c>...");

            try
            {
                using (var transmitter = CreateTransmitter())
                {
                    using (var engine = new ImuseEngine(transmitter, target, imuseOptions))
                    {
                        // Clean up, even with Ctrl+C
                        SetupCancelHandler(engine, transmitter);

                        engine.RegisterSound(0, soundFile);

                        engine.StartSound(0);
                        BuildCommands(engine);

                        transmitter.Start();

                        GameLoop(engine);

                        TearDownCancelHandler();
                    }
                }
            }
            catch (ImuseException ex)
            {
                throw new ImuseSequencerException(ex.Message, ex);
            }
        }

        private readonly Dictionary<char, HookInfo> hooksByKey = new Dictionary<char, HookInfo>();
        private readonly string hookChars = "1234567890abcdefghijklmnoprstuvwxy";

        private void BuildCommands(ImuseEngine engine)
        {
            var interactivityInfo = engine.Commands.GetInteractivityInfo(0);

            int index = 0;

            logger.Info("");
            logger.Info("Commands:");
            foreach (var hook in interactivityInfo.Hooks)
            {
                if (hook.Id == 0)
                {
                    // Hook 0 is unconditional
                    continue;
                }
                var c = hookChars[index];
                hooksByKey.Add(c, hook);
                logger.Info($"{c}: {hook}");
                index++;
            }
            logger.Info("z: clear-loop");
            logger.Info("q: quit");
            logger.Info("");
        }

        private void GameLoop(ImuseEngine engine)
        {
            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Z:
                        engine.Commands.ClearLoop(0);
                        break;

                    case ConsoleKey.Q:
                        return;

                    default:
                        if (hooksByKey.TryGetValue(keyInfo.KeyChar, out var hookInfo))
                        {
                            engine.Commands.SetHook(0, hookInfo.Type, hookInfo.Id, hookInfo.Channel);
                        }
                        break;
                }
            }

        }

        private void PlayToFile(SoundFile soundFile, SoundTarget target)
        {
            logger.Info($"Writing playback of <c#88cc55>{options.InputPath}</c> to <c#88cc55>{options.OutputPath}</c>...");

            try
            {
                var transmitter = new MidiFileWriterTransmitter();
                using (var engine = new ImuseEngine(transmitter, target, imuseOptions))
                {
                    // Clean up, even with Ctrl+C
                    SetupCancelHandler(engine, transmitter);

                    engine.RegisterSound(0, soundFile);
                    engine.StartSound(0);

                    transmitter.Start();

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
