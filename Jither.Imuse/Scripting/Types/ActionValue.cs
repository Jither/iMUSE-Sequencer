namespace Jither.Imuse.Scripting.Types
{
    public class ActionValue : RuntimeValue
    {
        public ImuseAction Value { get; }

        public ActionValue(ImuseAction action) : base(RuntimeType.Action)
        {
            Value = action;
        }

        public override string ToString()
        {
            return $"action {Value.Name ?? "<anonymous>"}{(Value.During == null ? "" : $" during {Value.During}")}";
        }
    }
}
