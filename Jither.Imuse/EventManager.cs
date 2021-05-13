using Jither.Imuse.Scripting.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse
{
    public abstract class ImuseEvent
    {
        public ImuseAction Action { get; }

        protected ImuseEvent(ImuseAction action)
        {
            Action = action;
        }
    }

    public class StartEvent : ImuseEvent
    {
        public StartEvent(ImuseAction action) : base(action)
        {

        }
    }

    public class TimedEvent : ImuseEvent
    {
        public Time Time { get; }

        public TimedEvent(Time time, ImuseAction action) : base(action)
        {
            Time = time;
        }
    }

    public class KeyPressEvent : ImuseEvent
    {
        public string Key { get; }

        public KeyPressEvent(string key, ImuseAction action) : base(action)
        {
            Key = key;
        }
    }

    public class EventManager
    {
        private readonly List<StartEvent> startEvents = new();
        private readonly List<TimedEvent> timedEvents = new();
        private readonly List<KeyPressEvent> keyPressEvents = new();

        public EventManager()
        {

        }

        public void RegisterEvent(ImuseEvent evt)
        {
            switch (evt)
            {
                case StartEvent start:
                    startEvents.Add(start);
                    break;
                case TimedEvent timed:
                    timedEvents.Add(timed);
                    break;
                case KeyPressEvent keyPress:
                    keyPressEvents.Add(keyPress);
                    break;
            }
        }
    }
}
