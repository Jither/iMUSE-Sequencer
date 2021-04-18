using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Jither.Midi.Sequencing
{
    public class MidiScheduler<T> : IDisposable where T: ISchedulable
    {
        private bool disposed;

        private readonly object lockTiming = new();
        private readonly object lockRun = new();
        private readonly object lockThread = new();

        private readonly Stopwatch stopwatch = new();

        private int microsecondsPerBeat = 500000;
        private readonly int ticksPerQuarterNote;

        // Accumulated offset due to tempo changes
        private long microsecondsOffset = 0;
        private bool isRunning = false;

        [ThreadStatic]
        private static bool isSchedulerThread;
        private bool cancelThread;

        // Tick currently being processed on scheduling thread
        private long currentTick = 0;

        private readonly ScheduleQueue<T> queue = new();
        private Thread thread = null;

        private long MicrosecondsPerTick => microsecondsPerBeat * ticksPerQuarterNote;
        private long ElapsedMicroseconds => stopwatch.ElapsedTicks * 1000_000 / Stopwatch.Frequency;

        public event Action<List<T>> SliceReached;
        public event Action<int> TempoChanged;

        /// <summary>
        /// Gets the scheduler's ticks per quarternote (aka Pulses Per Quarter Note - PPQN).
        /// </summary>
        public int TicksPerQuarterNote => ticksPerQuarterNote;

        /// <summary>
        /// Sets tempo in microseconds per beat ("MIDI tempo"). Default MIDI tempo (120bpm) = 500,000 microseconds per beat.
        /// </summary>
        public int MicrosecondsPerBeat
        {
            get => microsecondsPerBeat;
            set
            {
                UpdateTempo(value);
            }
        }

        /// <summary>
        /// Gets or sets tempo in beats per minute. Default MIDI tempo is 120bpm.
        /// </summary>
        public decimal BeatsPerMinute
        {
            get => 60_000_000m / microsecondsPerBeat;
            set
            {
                int tempo = (int)(60_000_000m / value);
                UpdateTempo(tempo);
            }
        }

        public long TimeInMicroseconds
        {
            get
            {
                if (isSchedulerThread)
                {
                    return currentTick * MicrosecondsPerTick;
                }
                lock (lockTiming)
                {
                    return ElapsedMicroseconds + microsecondsOffset;
                }
            }
        }

        public long TimeInTicks
        {
            get
            {
                if (isSchedulerThread)
                {
                    return currentTick;
                }
                lock (lockTiming)
                {
                    return (ElapsedMicroseconds + microsecondsOffset) / MicrosecondsPerTick;
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                if (isSchedulerThread)
                {
                    return true;
                }
                lock (lockRun)
                {
                    return isRunning;
                }
            }
        }

        public MidiScheduler(int microsecondsPerBeat, int ticksPerQuarterNote)
        {
            this.ticksPerQuarterNote = ticksPerQuarterNote;
            this.MicrosecondsPerBeat = microsecondsPerBeat;
        }

        public void Start()
        {
            if (isSchedulerThread)
            {
                throw new InvalidOperationException("MidiScheduler is already running");
            }
            lock (lockRun)
            {
                if (isRunning)
                {
                    throw new InvalidOperationException("MidiScheduler is already running");
                }

                stopwatch.Start();

                cancelThread = false;

                // TODO: Use Task
                thread = new Thread(new ThreadStart(ThreadRun));
                thread.Start();

                isRunning = true;
            }
        }

        public void Stop()
        {
            if (isSchedulerThread)
            {
                throw new InvalidOperationException("Cannot stop MidiScheduler from the scheduler thread.");
            }

            lock (lockRun)
            {
                if (!isRunning)
                {
                    throw new InvalidOperationException("MidiScheduler is not running.");
                }

                lock (lockThread)
                {
                    cancelThread = true;
                    Monitor.Pulse(lockThread);
                }

                thread.Join();
                thread = null;

                stopwatch.Stop();

                isRunning = false;
            }
        }

        public void Reset()
        {
            if (isSchedulerThread)
            {
                throw new InvalidOperationException("Cannot reset MidiScheduler while it's running.");
            }

            lock (lockRun)
            {
                if (isRunning)
                {
                    throw new InvalidOperationException("Cannot reset MidiScheduler while it's running.");
                }

                stopwatch.Reset();
                microsecondsOffset = 0;

                lock (lockThread)
                {
                    queue.Clear();
                    Monitor.Pulse(lockThread);
                }
            }
        }

        public void Schedule(T evt)
        {
            lock (lockThread)
            {
                queue.Add(evt);
                Monitor.Pulse(lockThread);
            }
        }

        public void Schedule(IReadOnlyList<T> events)
        {
            lock (lockThread)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    queue.Add(events[i]);
                }
                Monitor.Pulse(lockThread);
            }
        }

        private long MicrosecondsUntil(long tick)
        {
            long time = tick * microsecondsPerBeat / ticksPerQuarterNote;
            long now = ElapsedMicroseconds + microsecondsOffset;
            long delta = time - now;
            return delta >= 0 ? delta : 0;
        }

        private void ThreadRun()
        {
            isSchedulerThread = true;
            lock (lockThread)
            {
                while (true)
                {
                    if (cancelThread)
                    {
                        return;
                    }
                    if (queue.IsEmpty)
                    {
                        Monitor.Wait(lockThread);
                    }
                    else
                    {
                        long waitTime = MicrosecondsUntil(queue.EarliestTime);
                        if (waitTime > 0)
                        {
                            // Let other threads do work
                            Monitor.Wait(lockThread, (int)(waitTime / 1000));
                        }
                        else
                        {
                            currentTick = queue.EarliestTime;
                            var slice = queue.PopEarliest();
                            SliceReached(slice);
                        }
                    }
                }
            }
        }

        private void UpdateTempo(int newMicrosecondsPerBeat)
        {
            lock (lockTiming)
            {
                long currentMicroseconds = ElapsedMicroseconds;
                long currentOffsetMicroseconds = currentMicroseconds + microsecondsOffset;
                long newOffsetMicroseconds = currentOffsetMicroseconds * newMicrosecondsPerBeat / microsecondsPerBeat;
                long newOffset = newOffsetMicroseconds - currentMicroseconds;
                microsecondsPerBeat = newMicrosecondsPerBeat;
                microsecondsOffset = newOffset;
            }
            TempoChanged?.Invoke(microsecondsPerBeat);
            // Let scheduler thread apply new timing
            lock (lockThread)
            {
                Monitor.Pulse(lockThread);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                if (isRunning)
                {
                    Stop();
                    Reset();
                }
                GC.SuppressFinalize(this);
            }
        }
    }
}
