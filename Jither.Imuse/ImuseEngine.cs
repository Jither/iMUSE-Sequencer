using Jither.Imuse.Drivers;
using Jither.Imuse.Files;
using Jither.Imuse.Scripting.Events;
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
        private readonly Sustainer sustainer;
        private readonly PlayerManager players;
        private readonly PartsManager parts;
        private readonly FileManager files = new();

        private int ticksPerQuarterNote;

        private bool disposed;

        public ImuseQueue Queue { get; }
        public CommandManager Commands { get; }
        public EventManager Events { get; }

        public ImuseEngine(ITransmitter transmitter, SoundTarget target, ImuseOptions options = null)
        {
            this.transmitter = transmitter;
            transmitter.Engine = this;

            this.target = target;

            options ??= new ImuseOptions();

            driver = GetDriver();

            Queue = new ImuseQueue(this);

            parts = new PartsManager(driver, options);
            sustainer = new Sustainer(options);
            players = new PlayerManager(files, parts, sustainer, driver, Queue, options);

            Commands = new CommandManager(players);
            Events = new EventManager();
        }

        public void RegisterSound(int id, SoundFile file)
        {
            if (file.Midi.DivisionType != DivisionType.Ppqn)
            {
                throw new ImuseException($"iMUSE only supports PPQN division MIDI files - this appears to be SMPTE.");
            }

            if (this.ticksPerQuarterNote != 0)
            {
                if (file.Midi.TicksPerQuarterNote != this.ticksPerQuarterNote)
                {
                    throw new ImuseException($"Ticks per quarter note (PPQN) for sound '{file.Name}' differs from sounds registered earlier. Cannot register this sound.");
                }
            }
            else
            {
                this.ticksPerQuarterNote = file.Midi.TicksPerQuarterNote;
            }

            files.Register(id, file);
        }

        public void StartSound(int id)
        {
            players.StartSound(id);
        }

        public void Init()
        {
            transmitter.Init(ticksPerQuarterNote);
            driver.Init(ticksPerQuarterNote);
            // Send no-op MIDI message to indicate initialization is done.
            // When the no-op reaches the transmitter, it can use it as an indication that playback should
            // be started.
            driver.TransmitNoOp(Messages.NoOpSignal.Initialized);
        }

        public long Play(long ticks)
        {
            if (ticks <= 0)
            {
                ticks = Int64.MaxValue;
            }
            long currentTick = 0;

            // We use this no-op MIDI message to indicate the start of this batch of MIDI messages.
            // When the no-op reaches the transmitter, it can use it as an indication that it should start
            // preparing the next batch.
            driver.TransmitNoOp(Messages.NoOpSignal.ReadyForNextBatch);

            while (currentTick < ticks)
            {
                bool continuePlaying = players.Tick();
                continuePlaying = sustainer.Tick() || continuePlaying;
                driver.CurrentTick++;
                if (!continuePlaying)
                {
                    var sustainNotes = parts.GetSustainNotes();
                    foreach (var note in sustainNotes)
                    {
                        logger.Warning($"Still playing note: {note}");
                    }
                    break;
                }
                currentTick++;
            }
            // Return number of ticks played so far. Will be less than the requested number of ticks, if player
            // signaled a stop.
            // TODO: Pretending we got full amount of ticks every time. Temporary measure to let the engine start before anything is ready to play...
            // Plenty of issues with that, most notably:
            // * when done playing, will output a noop ("call me") every tick.
            return ticks; // currentTick;
        }

        public void Stop()
        {
            driver.Close();
        }

        private Driver GetDriver()
        {
            return target switch
            {
                SoundTarget.Roland => new Roland(transmitter),
                SoundTarget.GeneralMidi => new GeneralMidi(transmitter),
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
