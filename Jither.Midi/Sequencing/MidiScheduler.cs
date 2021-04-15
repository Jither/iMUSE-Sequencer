using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jither.Midi.Sequencing
{
    public class MidiScheduler : IDisposable
    {
        private bool disposed;
        private int microsecondsPerBeat = 500000;

        private readonly object lockTiming = new();
        private readonly object lockRun = new();
        private readonly object lockThread = new();

        private readonly Stopwatch stopwatch = new();
        private long jitter = 0;
        private bool isRunning = false;

        [ThreadStatic]
        private static bool isSchedulerThread;
        private bool cancelThread;

        private long threadProcessingTime = 0;
        private readonly ScheduleQueue<MidiEvent> queue = new();
        private Thread thread = null;

        private long ElapsedMicroseconds => stopwatch.ElapsedTicks * 1000_000 / Stopwatch.Frequency;

        public event Action<List<MidiEvent>> SliceReached;
        public event Action<int> TempoChanged;

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
        /// Sets tempo in beats per minute. Default MIDI tempo is 120bpm.
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
                    return threadProcessingTime;
                }
                lock (lockTiming)
                {
                    return ElapsedMicroseconds + jitter;
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

        public MidiScheduler(int microsecondsPerBeat)
        {
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
                jitter = 0;

                lock (lockThread)
                {
                    queue.Clear();
                    Monitor.Pulse(lockThread);
                }
            }
        }

        public void Schedule(MidiEvent evt)
        {
            lock (lockThread)
            {
                queue.Add(evt);
                Monitor.Pulse(lockThread);
            }
        }

        public void Schedule(IReadOnlyList<MidiEvent> events)
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
            long time = tick * microsecondsPerBeat / 480;
            long now = ElapsedMicroseconds + jitter;
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
                            Monitor.Wait(lockThread, (int)(waitTime / 1000));
                        }
                        else
                        {
                            threadProcessingTime = queue.EarliestTime;
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
                long currentJitteredMicroseconds = currentMicroseconds + jitter;
                double beatTime = currentJitteredMicroseconds / microsecondsPerBeat;
                long newJitteredMicroseconds = (long)(beatTime * newMicrosecondsPerBeat);
                long newJitter = newJitteredMicroseconds - currentMicroseconds;
                microsecondsPerBeat = newMicrosecondsPerBeat;
                jitter = newJitter;
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
            }
        }
    }
}
