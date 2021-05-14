namespace Jither.Imuse.Scripting.Types
{
    public class ActionValue : RuntimeValue
    {
        public ImuseAction Value { get; }
        public override object UntypedValue => Value;

        public ActionValue(ImuseAction action) : base(RuntimeType.Action)
        {
            Value = action;
        }

        public override string ToString()
        {
            return $"action {Value.Name ?? "<anonymous>"}{(Value.During == null ? "" : $" during {Value.During}")}";
        }

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is ActionValue action && action.Value == Value;
        }
    }
}
