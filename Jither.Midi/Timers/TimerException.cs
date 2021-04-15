using System;

namespace Jither.Midi.Timers
{
    public class TimerException : Exception
    {
        public TimerException(string message) : base(message)
        {
        }
    }
}
