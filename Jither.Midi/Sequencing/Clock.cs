using Jither.Midi.Timers;
using System;

namespace Jither.Midi.Sequencing
{
    public class Clock : IDisposable
    {
        private readonly ITimer timer;
        private readonly int interval;
        private int tempo = 500000; // Tempo in microseconds per beat
        private int intervalResolution;
        private int ticksPerClock;
        private int fractionalTicks = 0;
        private int ppqn = 24;
        private bool isRunning = false;
        private bool disposed = false;

        private int ticks = 0;

        public int Ticks => ticks;

        public int Tempo
        {
            get => tempo;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(Tempo));
                }
                tempo = value;
            }
        }

        public int Ppqn
        {
            get => ppqn;
            set
            {
                if (value < 24)
                {
                    throw new ArgumentOutOfRangeException(nameof(Ppqn), "Pulses per quarter note cannot be < 24");
                }

                ppqn = value;
                Update();
            }
        }

        public int TicksPerClock => ticksPerClock;

        public event Action Tick;
        public event Action Started;
        public event Action Resumed;
        public event Action Stopped;

        public bool IsActive => isRunning;

        public Clock(ITimer timer, int interval = 0)
        {
            this.timer = timer;
            if (interval < 1)
            {
                interval = timer.Capabilities.MinimumInterval;
            }
            this.interval = interval;
            timer.Mode = TimerMode.Interval;
            timer.Interval = interval;
            timer.Tick += Timer_Tick;
            Update();
        }

        public void Start()
        {
            if (isRunning)
            {
                return;
            }

            ticks = 0;
            Reset();

            Started?.Invoke();

            timer.Start();

            isRunning = true;
        }

        public void Resume()
        {
            if (isRunning)
            {
                return;
            }

            Resumed?.Invoke();

            timer.Start();

            isRunning = true;
        }

        public void Stop()
        {
            timer.Stop();
            isRunning = false;
            Stopped?.Invoke();
        }

        public void SetTicks(int ticks)
        {
            if (ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticks));
            }

            if (isRunning)
            {
                Stop();
            }

            this.ticks = ticks;

            Reset();
        }

        private void Timer_Tick()
        {
            int t = GenerateTicks();

            for (int i = 0; i < t; i++)
            {
                Tick?.Invoke();
                ticks++;
            }
        }

        private void Reset()
        {
            fractionalTicks = 0;
        }

        private void Update()
        {
            intervalResolution = ppqn * interval * 1000;
            ticksPerClock = ppqn / 24;
        }

        private int GenerateTicks()
        {
            int ticks = (fractionalTicks + intervalResolution) / tempo;
            fractionalTicks += intervalResolution - ticks * tempo;

            return ticks;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (isRunning)
            {
                timer.Stop();
            }

            disposed = true;

            timer.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
