using System;

namespace Jither.Imuse.Scripting.Types
{
    public class BooleanValue : RuntimeValue
    {
        public static readonly BooleanValue True = new(true);
        public static readonly BooleanValue False = new(false);
        public bool Value { get; }
        public override object UntypedValue => Value;

        private BooleanValue(bool value) : base(RuntimeType.Boolean)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value ? "true" : "false";
        }

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is BooleanValue boolean && boolean.Value == Value;
        }
    }
}
