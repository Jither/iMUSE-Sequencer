namespace Jither.Imuse.Scripting.Types
{
    public class NullValue : RuntimeValue
    {
        internal NullValue() : base(RuntimeType.Null)
        {

        }

        public override string ToString()
        {
            return "null";
        }
    }
}
