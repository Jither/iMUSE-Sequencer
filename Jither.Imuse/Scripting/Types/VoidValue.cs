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
    }
}
