using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime
{
    public enum ExecutionResultType
    {
        Normal,
        Break
    }

    public class ExecutionResult
    {
        public readonly static ExecutionResult Void = new(ExecutionResultType.Normal, RuntimeValue.Null);
        public readonly static ExecutionResult Break = new(ExecutionResultType.Break, RuntimeValue.Null);

        public ExecutionResultType Type { get; }
        public RuntimeValue Value { get; }
        public string Identifier { get; }

        public ExecutionResult(ExecutionResultType type, RuntimeValue value, string identifier = null)
        {
            Type = type;
            Value = value;
            Identifier = identifier;
        }
    }
}
