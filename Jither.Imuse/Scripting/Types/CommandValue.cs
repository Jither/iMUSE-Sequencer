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

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is CommandValue command && command.Value == Value;
        }
    }
}
