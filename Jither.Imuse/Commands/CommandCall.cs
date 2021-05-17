using Jither.Imuse.Scripting.Types;
using System.Collections.Generic;

namespace Jither.Imuse.Commands
{
    /// <summary>
    /// A precomposed call to a command - used e.g. in the Queue.
    /// </summary>
    public class CommandCall
    {
        public Command Command { get; }
        public IReadOnlyList<RuntimeValue> Arguments { get; }

        public CommandCall(Command command, IReadOnlyList<RuntimeValue> arguments)
        {
            Command = command;
            Arguments = arguments;
        }
    }
}
