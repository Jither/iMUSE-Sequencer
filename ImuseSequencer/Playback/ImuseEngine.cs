using ImuseSequencer.Drivers;
using ImuseSequencer.Parsing;
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

        private readonly ITransmitter transmitter;
        private readonly SoundTarget target;
        private readonly Driver driver;
        
        private readonly PlayerManager players;
        private readonly FileManager files = new();

        private bool disposed;

        public ImuseEngine(ITransmitter transmitter, SoundTarget target)
        {
            this.transmitter = transmitter;
            this.target = target;

            driver = GetDriver();

            logger.Info($"Target device: {target.GetFriendlyName()}");

            driver.Init();

            var parts = new PartsManager(driver);
            var sustainer = new Sustainer();
            players = new PlayerManager(files, parts, sustainer, driver);
        }

        public void RegisterSound(int id, SoundFile file)
        {
            if (file.Midi.DivisionType != DivisionType.Ppqn)
            {
                throw new ImuseSequencerException($"iMUSE Sequencer only supports PPQN division MIDI files - this appears to be SMPTE.");
            }

            transmitter.Init(file.Midi.TicksPerQuarterNote);

            files.Register(id, file);
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
        }

        public void Stop()
        {
            driver.Reset();
        }

        private Driver GetDriver()
        {
            return target switch
            {
                SoundTarget.Roland => new Roland(transmitter),
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

            GC.SuppressFinalize(this);
        }
    }
}
