using Jither.Imuse.Commands;
using Jither.Imuse.Drivers;
using Jither.Imuse.Files;
using Jither.Imuse.Parts;
using Jither.Imuse.Scripting.Events;
using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Types;
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
        private readonly ImuseOptions options;
        private readonly Driver driver;
        private readonly Sustainer sustainer;
        private readonly PlayerManager players;
        private readonly FileManager files = new();

        private bool isInitialized;
        private int ticksPerQuarterNote;
        private ImuseVersion imuseVersion;
        private SoundTarget target;
        private PartManager parts;

        private bool disposed;

        public ImuseQueue Queue { get; }
        public ImuseCommands Commands { get; }
        public EventManager Events { get; }

        public event Action Tick;

        public ImuseEngine(ITransmitter transmitter, SoundTarget target, ImuseOptions options = null)
        {
            this.transmitter = transmitter;
            transmitter.Engine = this;
            this.target = target;
            this.options = options;

            options ??= new ImuseOptions();

            driver = GetDriver(target);

            Queue = new ImuseQueue(this);
            sustainer = new Sustainer(options);
            players = new PlayerManager(files, Queue, options);

            Commands = new ImuseCommands(players, Queue);
            Events = new EventManager();
        }

        public void RegisterSound(int id, SoundFile file)
        {
            if (file.Midi.DivisionType != DivisionType.Ppqn)
            {
                throw new ImuseException($"iMUSE only supports PPQN division MIDI files - this appears to be SMPTE.");
            }

            if (!isInitialized)
            {
                Initialize(file);
            }
            else
            {
                CheckFileConsistency(file);
            }

            files.Register(id, file);
        }

        private void Initialize(SoundFile file)
        {
            this.ticksPerQuarterNote = file.Midi.TicksPerQuarterNote;
            this.imuseVersion = file.ImuseVersion;

            parts = imuseVersion == ImuseVersion.V1 ? new PartManagerV1(driver, options) : new PartManagerV2(driver, options);

            players.Init(driver, parts, sustainer);

            isInitialized = true;
        }

        private void CheckFileConsistency(SoundFile file)
        {
            if (file.ImuseVersion != this.imuseVersion)
            {
                throw new ImuseException($"iMUSE version for sound '{file.Name}' differs from sounds registered earlier. Cannot register this sound.");
            }

            if (file.Midi.TicksPerQuarterNote != this.ticksPerQuarterNote)
            {
                throw new ImuseException($"Ticks per quarter note (PPQN) for sound '{file.Name}' differs from sounds registered earlier. Cannot register this sound.");
            }
        }

        public void StartSound(int id)
        {
            players.StartSound(id);
        }

        public InteractivityInfo GetInteractivityInfo(int sound)
        {
            return files.Get(sound).GetInteractivityInfo();
        }

        // TODO: Can this be combined with Initialize?
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

            // We use this no-op MIDI message to indicate the start of this batch of MIDI messages.
            // When the no-op reaches the transmitter, it can use it as an indication that it should start
            // preparing the next batch.
            driver.TransmitNoOp(Messages.NoOpSignal.ReadyForNextBatch);

            long remainingTicks = ticks;
            while (remainingTicks > 0)
            {
                Tick?.Invoke();
                bool continuePlaying = players.Tick();
                continuePlaying = sustainer.Tick() || continuePlaying;
                driver.CurrentTick++;
                remainingTicks--;
                if (!continuePlaying)
                {
                    var sustainNotes = parts.GetSustainNotes();
                    foreach (var note in sustainNotes)
                    {
                        logger.Warning($"Still playing note: {note}");
                    }
                    // TODO: Temporary measure because the engine is constantly playing, and we don't want 1 tick between each no-op
                    driver.CurrentTick += remainingTicks;
                    break;
                }
            }
            // Return number of ticks played so far. Will be less than the requested number of ticks, if player
            // signaled a stop.
            // TODO: Pretending we got full amount of ticks every time. Temporary measure to let the engine start before anything is ready to play...
            // Plenty of issues with that, most notably:
            // * when done playing, will output a noop ("call me") every tick - which we "fix" by telling the driver to add the remaining ticks:
            return ticks; // currentTick;
        }

        public void Stop()
        {
            driver.Close();
        }

        private Driver GetDriver(SoundTarget target)
        {
            return target switch
            {
                SoundTarget.Roland => new Roland(transmitter),
                SoundTarget.GeneralMidi => new GeneralMidi(transmitter),
                _ => throw new ImuseException($"Driver for {target} target is not implemented yet."),
            };
        }

        public void PopulateExecutionContext(ExecutionContext context)
        {
            context.CurrentScope.AddSymbol("sound-mode", IntegerValue.Create((int)target));
            context.CurrentScope.AddSymbol("ADLIB", IntegerValue.Create((int)SoundTarget.Adlib));
            context.CurrentScope.AddSymbol("ROLAND", IntegerValue.Create((int)SoundTarget.Roland));
            context.CurrentScope.AddSymbol("PC", IntegerValue.Create((int)SoundTarget.Speaker));
            context.CurrentScope.AddSymbol("GMIDI", IntegerValue.Create((int)SoundTarget.GeneralMidi));
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
