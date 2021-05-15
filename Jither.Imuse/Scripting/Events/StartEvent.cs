using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Events
{
    public class StartEvent : ImuseEvent
    {
        public StartEvent(ImuseAction action) : base(action)
        {

        }
    }
}
