using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Events
{
    public class TimedEvent : ImuseEvent
    {
        public Time Time { get; }

        public TimedEvent(Time time, ImuseAction action) : base(action)
        {
            Time = time;
        }
    }
}
