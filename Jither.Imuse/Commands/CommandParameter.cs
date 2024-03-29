﻿using Jither.Imuse.Scripting.Types;
using Jither.Utilities;

namespace Jither.Imuse.Commands
{
    public class CommandParameter
    {
        public string Name { get; }
        public RuntimeType Type { get; }

        public CommandParameter(string name, RuntimeType type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Type.GetDisplayName()} {Name}";
        }
    }
}
