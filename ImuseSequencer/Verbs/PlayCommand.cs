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

namespace ImuseSequencer.Verbs
{
    [Verb("play", Help = "Plays file")]
    public class PlayOptions
    {
        [Positional(0, Name = "input file", Help = "Input MIDI file (Standard MIDI file or LEC chunk - SOUN, SOU, ADL, ROL etc.)", Required = true)]
        public string InputPath { get; set; }

        [Option('o', "output", Help = "ID of MIDI output device.", ArgName = "device id", Required = true)]
        public int DeviceId { get; set; }

        [Option('t', "target", Help = "Playback target device. 'Unknown' will determine from LEC chunk, if present.", ArgName = "target", Default = SoundTarget.Unknown)]
        public SoundTarget Target { get; set; }
    }

    public class PlayCommand
    {
        private readonly Logger logger = LogProvider.Get(nameof(PlayCommand));

        private readonly PlayOptions options;

        public PlayCommand(PlayOptions options)
        {
            this.options = options;
        }

        public void Execute()
        {
            var midiFile = new MidiFile(options.InputPath);
            var target = options.Target == SoundTarget.Unknown ? midiFile.Target : options.Target;
            if (target == SoundTarget.Unknown)
            {
                throw new ImuseSequencerException("Unable to determine target device. Please specify it as an argument.");
            }

            logger.Info($"Playing {options.InputPath}");
            logger.Info($"Target device: {target.GetFriendlyName()}");

            using (var output = new WindowsOutputDevice(options.DeviceId))
            {
                using (var scheduler = new MidiScheduler(500000))
                {
                    scheduler.Schedule(midiFile.Tracks[0].Events);
                    scheduler.SliceReached += slice =>
                    {
                        for (int i = 0; i < slice.Count; i++)
                        {
                            var message = slice[i].Message;
                            if (message is SetTempoMessage meta)
                            {
                                scheduler.MicrosecondsPerBeat = meta.Tempo;
                            }
                            else
                            {
                                logger.Info(message.ToString());
                                output.SendMessage(message);
                            }
                        }
                    };
                    scheduler.TempoChanged += tempo =>
                    {
                        logger.Info(tempo.ToString());
                    };
                    scheduler.Start();

                    Console.ReadKey();
                }
            }
        }
    }
}
