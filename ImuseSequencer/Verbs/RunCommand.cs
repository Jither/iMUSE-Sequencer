using ImuseSequencer.Helpers;
using ImuseSequencer.Playback;
using ImuseSequencer.UI;
using Jither.CommandLine;
using Jither.Imuse;
using Jither.Imuse.Scripting;
using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Runtime;
using Jither.Logging;
using Jither.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    [Verb("run", Help = "Runs an iMUSE script")]
    public class RunOptions : CommonPlaybackOptions
    {
        [Positional(0, Name ="script path", Help = "Path to iMUSE script", Required = true)]
        public string ScriptPath { get; set; }

        [Option('o', "output", Help = "MIDI output device selector. If not specified, device in settings.json will be used, based on target.", ArgName = "selector")]
        public string DeviceSelector { get; set; }

        [Option('t', "target", Help = "Playback target device.", ArgName = "target", Required = true)]
        public SoundTarget Target { get; set; }
    }

    public class RunCommand : Command<RunOptions>
    {
        public RunCommand(Settings settings, RunOptions options) : base(settings, options)
        {
        }

        public override void Execute()
        {
            var source = File.ReadAllText(options.ScriptPath, Encoding.UTF8);

            try
            {
                Play(source);
            }
            catch (Exception ex) when (ex is ScriptException exScript)
            {
                logger.Error(ex.Message);
                OutputSourceWithLocation(source, exScript.Range);
            }
        }

        private void Play(string source)
        {
            var uiHandler = new UIHandler();

            var fileProvider = new FileProvider(settings.MidiFolder, options.Target);
            var interpreter = new Interpreter(source, fileProvider);

            options.DeviceSelector = OutputHelpers.Instance.DetermineDeviceSelector(options.DeviceSelector, options.Target);

            try
            {
                using (var transmitter = OutputHelpers.Instance.CreateTransmitter(options.DeviceSelector, options.Latency))
                {
                    using (var engine = new ImuseEngine(transmitter, options.Target, options.ImuseOptions))
                    {
                        // Clean up, even with Ctrl+C
                        ConsoleHelpers.SetupCancelHandler(engine, transmitter);

                        logger.Info($"Target device: [green]{options.Target.GetDisplayName()}[/]");
                        logger.Info($"Outputting to: [green]{transmitter.OutputName}[/]");
                        logger.Info("");

                        interpreter.Execute(engine);

                        engine.Tick += interpreter.Tick;

                        logger.Info("");

                        foreach (var evt in engine.Events.KeyPressEvents)
                        {
                            uiHandler.RegisterKeyPress(evt.Key, evt.Action.Name, key => { engine.Events.TriggerKey(key, interpreter.Context); return true; });
                        }
                        uiHandler.RegisterKeyPress("esc", "Quit", key => false);

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

        private void OutputSourceWithLocation(string source, SourceRange range)
        {
            var builder = new StringBuilder();
            builder.Append(source[..range.Start.Index]);
            builder.Append("[red]");
            builder.Append(source[range.Start.Index..range.End.Index]);
            builder.Append("[/]");
            builder.Append(source[range.End.Index..]);
            logger.Info(builder.ToString());
        }
    }
}
