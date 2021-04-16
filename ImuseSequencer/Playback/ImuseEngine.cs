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
        
        private MidiScheduler scheduler;
        
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

            players = new PlayerManager(files, driver);
        }

        public void RegisterSound(int id, MidiFile file)
        {
            if (file.DivisionType != DivisionType.Ppqn)
            {
                throw new ImuseSequencerException($"iMUSE Sequencer only supports PPQN division MIDI files - this appears to be SMPTE.");
            }
            
            if (scheduler == null)
            {
                scheduler = new MidiScheduler(500000, file.TicksPerQuarterNote);
            }
            else
            {
                if (file.TicksPerQuarterNote != scheduler.TicksPerQuarterNote)
                {
                    throw new ImuseSequencerException($"File '{file.Name}' has a PPQN (ticks per quarter note) value that differs from already registered files - it cannot be registered with them.");
                }
            }

            files.Register(id, file);
        }

        public void StartSound(int id)
        {
            players.StartSound(id);
        }

        public void Play()
        {
            scheduler.Schedule(files.Get(0).Tracks[0].Events);
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
            scheduler?.Dispose();
            output.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
