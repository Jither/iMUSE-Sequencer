using ImuseSequencer.Drivers;
using Jither.Logging;
using Jither.Midi.Devices;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Messages;
using Jither.Midi.Parsing;
using Jither.Midi.Sequencing;
using Jither.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public class ImusePlayer : IDisposable
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ImusePlayer));

        private readonly int deviceId;
        private readonly MidiFile file;
        private readonly SoundTarget target;
        private readonly Driver driver;
        private readonly OutputDevice output;
        private readonly MidiScheduler scheduler;

        private bool disposed;

        public ImusePlayer(int deviceId, MidiFile file, SoundTarget target)
        {
            this.deviceId = deviceId;
            this.file = file;
            this.target = target;
            
            if (file.DivisionType != DivisionType.Ppqn)
            {
                throw new ImuseSequencerException($"iMUSE Sequencer only supports PPQN division MIDI files - this appears to be SMPTE.");
            }

            output = new WindowsOutputDevice(deviceId);
            driver = GetDriver(output);
            scheduler = new MidiScheduler(500000, file.TicksPerQuarterNote);
        }

        public void Play()
        {
            logger.Info($"Target device: {target.GetFriendlyName()}");

            driver.Init();

            scheduler.Schedule(file.Tracks[0].Events);
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
                        logger.Info($"{scheduler.TimeInTicks,10} {message}");
                        output.SendMessage(message);
                    }
                }
            };
            scheduler.TempoChanged += tempo =>
            {
                logger.Info(tempo.ToString());
            };
            scheduler.Start();
        }

        public void Stop()
        {
            driver.Reset();
        }

        private Driver GetDriver(OutputDevice output)
        {
            return target switch
            {
                SoundTarget.Roland => new Roland(output),
                _ => throw new ImuseSequencerException($"Driver for {target} target is not implemented yet."),
            };
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            Stop();
            scheduler.Dispose();
            output.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
