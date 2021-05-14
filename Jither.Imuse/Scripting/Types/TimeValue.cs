namespace Jither.Imuse.Scripting.Types
{
    public class TimeValue : RuntimeValue
    {
        public Time Value { get; }
        public override object UntypedValue => Value;

        public TimeValue(Time value) : base(RuntimeType.Time)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
