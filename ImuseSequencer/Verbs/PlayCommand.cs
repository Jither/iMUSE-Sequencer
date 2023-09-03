using Jither.CommandLine;
using Jither.Logging;
using System;
using System.Collections.Generic;
using ImuseSequencer.Playback;
using System.IO;
using Jither.Imuse;
using Jither.Imuse.Files;
using System.Linq;
using ImuseSequencer.Helpers;
using ImuseSequencer.UI;
using Jither.Utilities;

namespace ImuseSequencer.Verbs;

[Verb("play", Help = "Plays file")]
public class PlayOptions : CommonPlaybackOptions
{
    [Positional(0, Name = "input file", Help = "Path to input MIDI file (Standard MIDI file or LEC chunk - SOUN, SOU, ADL, ROL etc.)", Required = true)]
    public string InputPath { get; set; }

    [Positional(1, Name = "output file", Help = "Path to output MIDI file. Cannot be combined with output to device.")]
    public string OutputPath { get; set; }

    [Option('o', "output", Help = "MIDI output device selector. Cannot be combined with output to file. If not specified, settings.json will be used, based on target.", ArgName = "selector")]
    public string DeviceSelector { get; set; }

    [Option('t', "target", Help = "Playback target device. 'Unknown' will determine from LEC chunk, if present.", ArgName = "target", Default = SoundTarget.Unknown)]
    public SoundTarget Target { get; set; }

    [Examples]
    public static IEnumerable<Example<PlayOptions>> Examples => new[]
    {
        new Example<PlayOptions>("Play file using MIDI output device 2", new PlayOptions { InputPath = "LARGO.rol", DeviceSelector = "2" }),
        new Example<PlayOptions>("Play file with MT-32 as target", new PlayOptions { InputPath = "OFFICE.mid", DeviceSelector = "2", Target = SoundTarget.Roland })
    };

    public bool ToFile => OutputPath != null;
    public bool ToDevice => DeviceSelector != null;

    public override void AfterParse()
    {
        if (ToFile && ToDevice)
        {
            throw new CustomParserException("Cannot output to both device and MIDI file at the same time. Pick one.");
        }

        if (ToFile)
        {
            // File output has its own limits
            if (JumpLimit == 0)
            {
                JumpLimit = 3;
            }
            if (LoopLimit == 0)
            {
                LoopLimit = 3;
            }
        }

        base.AfterParse();
    }
}

public class PlayCommand : Command<PlayOptions>
{
    public PlayCommand(Settings settings, PlayOptions options) : base(settings, options)
    {
    }

    public override void Execute()
    {
        SoundFile soundFile;
        try
        {
            soundFile = SoundFile.Load(options.InputPath);
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

        if (options.ToFile)
        {
            PlayToFile(soundFile, target);
        }
        else
        {
            PlayToDevice(soundFile, target);
        }
    }

    private void PlayToDevice(SoundFile soundFile, SoundTarget target)
    {
        logger.Info($"Playing [green]{options.InputPath}[/]...");
        var uiHandler = new UIHandler();

        options.DeviceSelector = OutputHelpers.Instance.DetermineDeviceSelector(options.DeviceSelector, target);

        try
        {
            using (var transmitter = OutputHelpers.Instance.CreateTransmitter(options.DeviceSelector, options.Latency))
            {
                using (var engine = new ImuseEngine(transmitter, target, options.ImuseOptions))
                {
                    // Clean up, even with Ctrl+C
                    ConsoleHelpers.SetupCancelHandler(engine, transmitter);

                    logger.Info($"Target device: [green]{target.GetDisplayName()}[/]");
                    logger.Info($"Outputting to: [green]{transmitter.OutputName}[/]");
                    logger.Info($"iMUSE version: [green]{soundFile.ImuseVersion.GetDisplayName()}[/]");
                    logger.Info("");

                    engine.RegisterSound(0, soundFile);
                    engine.StartSound(0);

                    logger.Info("");

                    BuildCommands(engine, uiHandler);
                    uiHandler.OutputMenu();

                    transmitter.Start();

                    uiHandler.Run();

                    ConsoleHelpers.TearDownCancelHandler();
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
        logger.Info($"Writing playback of [green]{options.InputPath}[/] to [green]{options.OutputPath}[/]...");

        try
        {
            var transmitter = new MidiFileWriterTransmitter();
            using (var engine = new ImuseEngine(transmitter, target, options.ImuseOptions))
            {
                // Clean up, even with Ctrl+C
                ConsoleHelpers.SetupCancelHandler(engine, transmitter);

                engine.RegisterSound(0, soundFile);
                engine.StartSound(0);

                transmitter.Start();

                transmitter.Write(options.OutputPath);

                ConsoleHelpers.TearDownCancelHandler();
            }
        }
        catch (ImuseException ex)
        {
            throw new ImuseSequencerException(ex.Message, ex);
        }
    }

    private readonly string hookChars = "1234567890abcdefghijklmnoprstuvwxy";

    private void BuildCommands(ImuseEngine engine, UIHandler handler)
    {
        var interactivityInfo = engine.GetInteractivityInfo(0);

        int index = 0;

        foreach (var hook in interactivityInfo.Hooks)
        {
            if (hook.Id == 0)
            {
                // Hook 0 is unconditional
                continue;
            }
            var c = hookChars[index];
            handler.RegisterKeyPress(c.ToString(), $"{hook}", key => { engine.Commands.SetHook(0, hook.Type, hook.Id, hook.Channel); return true; });
            index++;
        }
        if (interactivityInfo.SetLoops.Any())
        {
            var c = hookChars[index];
            handler.RegisterKeyPress(c.ToString(), "clear-loop", key => { engine.Commands.ClearLoop(0); return true; });
        }

        handler.RegisterKeyPress("esc", "Quit", key => false);
    }
}
