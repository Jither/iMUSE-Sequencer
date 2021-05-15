using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Events
{
    public abstract class ImuseEvent
    {
        public ImuseAction Action { get; }

        protected ImuseEvent(ImuseAction action)
        {
            Action = action;
        }
    }
}
