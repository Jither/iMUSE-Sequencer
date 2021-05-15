using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Events
{
    public class KeyPressEvent : ImuseEvent
    {
        public string Key { get; }

        public KeyPressEvent(string key, ImuseAction action) : base(action)
        {
            Key = key;
        }
    }
}
