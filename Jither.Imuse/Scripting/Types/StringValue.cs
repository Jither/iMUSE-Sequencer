namespace Jither.Imuse.Scripting.Types
{
    public class StringValue : RuntimeValue
    {
        public string Value { get; }
        public override object UntypedValue => Value;

        public StringValue(string value) : base(RuntimeType.String)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
