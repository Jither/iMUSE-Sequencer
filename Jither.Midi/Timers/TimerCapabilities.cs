namespace Jither.Midi.Timers
{
    public class TimerCapabilities
    {
        /// <summary>
        /// Minimum supported interval in milliseconds.
        /// </summary>
        public int MinimumInterval { get; }

        /// <summary>
        /// Maximum supported period in milliseconds.
        /// </summary>
        public int MaximumInterval { get; }

        internal TimerCapabilities(TimerCaps caps)
        {
            MinimumInterval = caps.periodMin;
            MaximumInterval = caps.periodMax;
        }
    }
}
