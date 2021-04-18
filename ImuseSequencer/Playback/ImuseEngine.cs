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
    public class ImuseEngine : IDisposable
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ImuseEngine));

        private readonly int deviceId;
        private readonly SoundTarget target;
        private readonly Driver driver;
        private readonly OutputDevice output;
        
        private MidiScheduler<MidiEvent> scheduler;
        
        private readonly PlayerManager players;
        private readonly FileManager files = new();
        private readonly PartsManager parts;

        private bool disposed;

        public ImuseEngine(int deviceId, SoundTarget target)
        {
            this.deviceId = deviceId;
            this.target = target;

            try
            {
                output = new WindowsOutputDevice(deviceId);
            }
            catch (MidiDeviceException ex)
            {
                throw new ImuseSequencerException($"Failed to connect to output: {ex.Message}");
            }
            SetupScheduler(480);
            driver = GetDriver(output, scheduler);

            logger.Info($"Target device: {target.GetFriendlyName()}");

            driver.Init();

            parts = new PartsManager(driver);
            players = new PlayerManager(files, parts, driver);
        }

        public void RegisterSound(int id, MidiFile file)
        {
            if (file.DivisionType != DivisionType.Ppqn)
            {
                throw new ImuseSequencerException($"iMUSE Sequencer only supports PPQN division MIDI files - this appears to be SMPTE.");
            }

            SetupScheduler(file.TicksPerQuarterNote);

            files.Register(id, file);
        }

        private void SetupScheduler(int ticksPerQuarterNote)
        {
            if (scheduler == null)
            {
                scheduler = new MidiScheduler<MidiEvent>(500000, ticksPerQuarterNote);
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
                    logger.Info($"{scheduler.TimeInTicks,10} tempo = {scheduler.BeatsPerMinute:0.00}");
                };
            }
            else
            {
                if (ticksPerQuarterNote != scheduler.TicksPerQuarterNote)
                {
                    throw new ImuseSequencerException($"File has a PPQN (ticks per quarter note) value ({ticksPerQuarterNote}) that differs from the scheduler's PPQN ({scheduler.TicksPerQuarterNote}) - it cannot be registered.");
                }
            }
        }

        public void StartSound(int id)
        {
            players.StartSound(id);
        }

        public void Play()
        {
            StartSound(0);

            bool done;
            do
            {
                done = players.Tick();
                driver.CurrentTick++;
            }
            while (!done);

            scheduler.Start();
        }

        public void Stop()
        {
            driver.Reset();
        }

        private Driver GetDriver(OutputDevice output, MidiScheduler<MidiEvent> scheduler)
        {
            return target switch
            {
                SoundTarget.Roland => new Roland(output, scheduler),
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
            scheduler?.Dispose();
            Stop();
            output.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
