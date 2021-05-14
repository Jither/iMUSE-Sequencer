namespace Jither.Imuse.Scripting.Types
{
    public class NullValue : RuntimeValue
    {
        public override object UntypedValue => null;

        internal NullValue() : base(RuntimeType.Null)
        {

        }

        public override string ToString()
        {
            return "null";
        }

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is NullValue;
        }
    }
}
