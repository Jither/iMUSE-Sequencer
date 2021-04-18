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

            driver = GetDriver(output);

            logger.Info($"Target device: {target.GetFriendlyName()}");

            driver.Init();

            var parts = new PartsManager(driver);
            var sustainer = new Sustainer();
            players = new PlayerManager(files, parts, sustainer, driver);
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
                            logger.Verbose($"{scheduler.TimeInTicks,10} {message}");
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

            // TODO: Preparation for interactive mode. Collect events before sending them to MidiScheduler to reduce amount of locks.
            var events = new List<MidiEvent>();
            driver.Transmitter = evt => { events.Add(evt); };

            bool done;
            do
            {
                done = players.Tick();
                driver.CurrentTick++;
            }
            while (!done);

            scheduler.Schedule(events);

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
            scheduler?.Dispose();
            Stop();
            output.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
