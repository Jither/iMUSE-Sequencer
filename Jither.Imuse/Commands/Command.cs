using Jither.Imuse.Helpers;
using Jither.Imuse.Scripting.Types;
using Jither.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Commands
{
    public class Command
    {
        public string Name { get; }
        public List<CommandParameter> Parameters { get; }
        public RuntimeType ReturnType { get; }
        public bool IsEnqueuable { get; }
        private readonly CommandMethod method;

        public Command(string name, List<CommandParameter> parameters, RuntimeType returnType, CommandMethod method, bool enqueuable)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returnType;
            this.method = method;
            IsEnqueuable = enqueuable;
        }

        public RuntimeValue Execute(IReadOnlyList<RuntimeValue> arguments)
        {
            object[] args = new object[arguments.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = arguments[i].UntypedValue;
            }
            object result = method(args);

            return ReturnType switch
            {
                RuntimeType.Boolean => (bool)result ? BooleanValue.True : BooleanValue.False,
                RuntimeType.Integer => IntegerValue.Create((int)result),
                RuntimeType.Void => RuntimeValue.Void,
                RuntimeType.String => new StringValue((string)result),
                RuntimeType.Time => new TimeValue((Time)result),
                _ => throw new NotImplementedException($"No conversion implemented for command return type {ReturnType}"),
            };
        }

        public override string ToString()
        {
            var prms = string.Join(", ", Parameters.Select(p => $"{p}"));
            return $"{ReturnType.GetDisplayName()} {Name} ({prms})";
        }
    }
}
