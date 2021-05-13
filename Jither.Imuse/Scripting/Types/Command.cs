using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Types
{
    public class Command
    {
        public string Name { get; }
        public List<CommandParameter> Parameters { get; }

        public Command(string name, List<CommandParameter> parameters)
        {
            Name = name;
            Parameters = parameters;
        }

        public RuntimeValue Execute(List<RuntimeValue> arguments)
        {
            throw new NotImplementedException();
        }
    }
}
