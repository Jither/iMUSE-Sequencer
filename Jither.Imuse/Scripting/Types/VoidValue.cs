namespace Jither.Imuse.Scripting.Types
{
    public class VoidValue : RuntimeValue
    {
        public override object UntypedValue => null;

        public VoidValue() : base(RuntimeType.Void)
        {

        }

        public override string ToString()
        {
            return "void";
        }

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is VoidValue;
        }
    }
}
