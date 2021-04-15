using System;

namespace Jither.Midi.Timers
{
    public interface ITimer : IDisposable
    {
        TimerCapabilities Capabilities { get; }

        TimerMode Mode { get; set; }
        int Interval { get; set; }
        int Resolution { get; set; }

        bool IsActive { get; }

        public event Action Started;
        public event Action Stopped;
        public event Action Tick;

        void Start();
        void Stop();
    }
}
