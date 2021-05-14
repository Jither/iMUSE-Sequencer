using Jither.Utilities;
using System.Linq;

namespace Jither.Imuse.Scripting.Types
{
    public class CommandValue : RuntimeValue
    {
        public Command Value { get; }
        public override object UntypedValue => Value;

        public CommandValue(Command value) : base(RuntimeType.Command)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"command {Value}";
        }
    }
}
