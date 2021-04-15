using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Timers
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct TimerCaps
    {
        public int periodMin;
        public int periodMax;

        public static TimerCaps Default
        {
            get
            {
                return new TimerCaps { periodMin = 1, periodMax = Int32.MaxValue };
            }
        }
    }

    public class WinApiMultimediaTimer : ITimer
    {
        private delegate void TimerCallback(int id, int message, int user, int param1, int param2);

#pragma warning disable IDE1006 // Naming Styles - keeping case of WinAPI functions

        [DllImport("winmm.dll")]
        private static extern int timeGetDevCaps(ref TimerCaps caps, int sizeOfTimerCaps);
        [DllImport("winmm.dll")]
        private static extern int timeSetEvent(int delay, int resolution, TimerCallback proc, IntPtr user, int mode);
        [DllImport("winmm.dll")]
        private static extern int timeKillEvent(int id);

#pragma warning restore IDE1006 // Naming Styles

        private const int TIMERR_NOERROR = 0;

        private int id;
        private volatile int interval;
        private volatile int resolution;
        private readonly TimerCallback callbackInterval;
        private readonly TimerCallback callbackSingle;
        private TimerMode mode;

        private bool isActive = false;

        private volatile bool disposed = false;

        private static TimerCaps capabilities;

        public TimerCapabilities Capabilities { get; }

        public int Interval
        {
            get => interval;
            set {
                if (value < capabilities.periodMin || value > capabilities.periodMax)
                {
                    throw new ArgumentOutOfRangeException(nameof(Interval));
                }
                interval = value;
                RestartIfRunning();
            }
        }

        public int Resolution
        {
            get => resolution;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Resolution));
                }
                resolution = value;
                RestartIfRunning();
            }
        }

        public TimerMode Mode
        {
            get => mode;
            set
            {
                mode = value;
                RestartIfRunning();
            }
        }

        public bool IsActive => isActive;

        public event Action Started;
        public event Action Stopped;
        public event Action Tick;

        static WinApiMultimediaTimer()
        {
            timeGetDevCaps(ref capabilities, Marshal.SizeOf<TimerCaps>());
        }

        public WinApiMultimediaTimer()
        {
            Capabilities = new TimerCapabilities(capabilities);

            this.interval = capabilities.periodMin;
            this.resolution = 1;

            isActive = false;

            callbackInterval =IntervalCallback;
            callbackSingle = SingleCallback;
        }

        ~WinApiMultimediaTimer()
        {
            if (isActive)
            {
                timeKillEvent(id);
                isActive = false;
            }
        }

        public void Start()
        {
            if (isActive)
            {
                return;
            }

            if (Mode == TimerMode.Interval)
            {
                id = timeSetEvent(interval, resolution, callbackInterval, IntPtr.Zero, (int)Mode);
            }
            else
            {
                id = timeSetEvent(interval, resolution, callbackSingle, IntPtr.Zero, (int)Mode);
            }

            if (id != 0)
            {
                isActive = true;
                Started?.Invoke();
            }
            else
            {
                throw new TimerException("An error occurred while starting WinAPI multimedia timer.");
            }
        }

        public void Stop()
        {
            if (!isActive)
            {
                return;
            }

            int result = timeKillEvent(id);
            Debug.Assert(result == TIMERR_NOERROR);

            isActive = false;

            Stopped?.Invoke();
        }

        private void IntervalCallback(int id, int message, int user, int param1, int param2)
        {
            if (disposed)
            {
                return;
            }

            Tick?.Invoke();
        }

        private void SingleCallback(int id, int message, int user, int param1, int param2)
        {
            if (disposed)
            {
                return;
            }

            Tick?.Invoke();
            Stop();
        }

        private void RestartIfRunning()
        {
            if (isActive)
            {
                Stop();
                Start();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            if (isActive)
            {
                timeKillEvent(id);
            }
        }
    }
}
