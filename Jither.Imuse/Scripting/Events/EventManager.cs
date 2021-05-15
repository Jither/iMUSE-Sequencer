using Jither.Imuse.Scripting.Runtime;
using Jither.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Events
{
    public class EventManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(EventManager));
        private readonly List<StartEvent> startEvents = new();
        private readonly List<TimedEvent> timedEvents = new();
        private readonly Dictionary<KeyPress, KeyPressEvent> keyPressEventsByKey = new();

        public List<KeyPressEvent> KeyPressEvents { get; } = new();

        public EventManager()
        {

        }

        public void TriggerStart(ExecutionContext context)
        {
            foreach (var evt in startEvents)
            {
                evt.Action.Execute(context);
            }
        }

        public void TriggerKey(KeyPress key, ExecutionContext context)
        {
            if (keyPressEventsByKey.TryGetValue(key, out var evt))
            {
                logger.Info($"Key {key} pressed - running action {evt.Action}");
                evt.Action.Execute(context);
            }
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
                    RegisterKeyPressEvent(keyPress);
                    break;
            }
        }

        private void RegisterKeyPressEvent(KeyPressEvent evt)
        {
            if (!KeyPress.TryParse(evt.Key, out var keyPress))
            {
                throw new EventException($"Unrecognized key specifier '{evt.Key}'. Example of valid key specifier: 'Shift+Ctrl+M'");
            }
            if (keyPressEventsByKey.ContainsKey(keyPress))
            {
                throw new EventException($"An event is already registered for keypress {keyPress}");
            }

            KeyPressEvents.Add(evt);
            keyPressEventsByKey.Add(keyPress, evt);
        }
    }
}
