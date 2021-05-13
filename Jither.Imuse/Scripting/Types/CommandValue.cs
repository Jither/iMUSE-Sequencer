using System.Linq;

namespace Jither.Imuse.Scripting.Types
{
    public class CommandValue : RuntimeValue
    {
        public Command Value { get; }

        public CommandValue(Command value) : base(RuntimeType.Command)
        {
            Value = value;
        }

        public override string ToString()
        {
            var prms = string.Join(", ", Value.Parameters.Select(p => $"{p}"));
            return $"command {Value.Name} ({prms})";
        }
    }
}
