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

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is TimeValue time && time.Value == Value;
        }
    }
}
