using System.Globalization;

namespace Jither.Imuse.Scripting.Types
{
    public class IntegerValue : RuntimeValue
    {
        public int Value { get; }

        public IntegerValue(int value) : base(RuntimeType.Integer)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
