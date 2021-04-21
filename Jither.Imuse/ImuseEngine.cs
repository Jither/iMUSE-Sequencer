﻿using Jither.Imuse.Drivers;
using Jither.Imuse.Files;
using Jither.Logging;
using Jither.Midi.Files;
using Jither.Utilities;
using System;

namespace Jither.Imuse
{
    public class ImuseEngine : IDisposable
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ImuseEngine));

        private readonly ITransmitter transmitter;
        private readonly SoundTarget target;
        private readonly Driver driver;
        
        private readonly PlayerManager players;
        private readonly FileManager files = new();

        private int? ticksPerQuarterNote;

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
                throw new ImuseException($"iMUSE only supports PPQN division MIDI files - this appears to be SMPTE.");
            }

            if (this.ticksPerQuarterNote != null)
            {
                if (file.Midi.TicksPerQuarterNote != this.ticksPerQuarterNote)
                {
                    throw new ImuseException($"Ticks per quarter note (PPQN) for sound '{file.Name}' differs from sounds registered earlier. Cannot register this sound.");
                }
            }
            else
            {
                this.ticksPerQuarterNote = file.Midi.TicksPerQuarterNote;
                // First file - we have a PPQN, so we can initialize:
                transmitter.Init(file.Midi.TicksPerQuarterNote);
            }

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
                _ => throw new ImuseException($"Driver for {target} target is not implemented yet."),
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