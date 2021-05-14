using System.Globalization;

namespace Jither.Imuse.Scripting.Types
{
    public class IntegerValue : RuntimeValue
    {
        private const int CommonValueCount = 200;

        public int Value { get; }
        public override object UntypedValue => Value;

        private static readonly IntegerValue[] commonValues = new IntegerValue[CommonValueCount];

        static IntegerValue()
        {
            // Cache the most common values
            for (int i = 0; i < CommonValueCount; i++)
            {
                commonValues[i] = new IntegerValue(i);
            }
        }

        protected IntegerValue(int value) : base(RuntimeType.Integer)
        {
            Value = value;
        }

        public static IntegerValue Create(int value)
        {
            if (value >= 0 && value < CommonValueCount)
            {
                return commonValues[value];
            }
            return new IntegerValue(value);
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is IntegerValue integer && integer.Value == Value;
        }
    }
}
