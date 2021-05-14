using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Jither.Imuse.Scripting.Types
{
    public enum RuntimeType
    {
        [EnumMember(Value = "void")]
        Void,
        [EnumMember(Value = "bool")]
        Boolean,
        [EnumMember(Value = "string")]
        String,
        [EnumMember(Value = "int")]
        Integer,

        [EnumMember(Value = "action")]
        Action,
        [EnumMember(Value = "command")]
        Command,
        [EnumMember(Value = "time")]
        Time,

        [EnumMember(Value = "null")]
        Null
    }

    public static class RuntimeTypes
    {
        private static readonly Dictionary<Type, RuntimeType> runtimeTypesByType = new()
        {
            [typeof(void)] = RuntimeType.Void,
            [typeof(int)] = RuntimeType.Integer,
            [typeof(string)] = RuntimeType.String,
            [typeof(bool)] = RuntimeType.Boolean,
            [typeof(Time)] = RuntimeType.Time
        };

        public static RuntimeType FromClrType(Type type)
        {
            if (!runtimeTypesByType.TryGetValue(type, out var value))
            {
                throw new NotSupportedException($"No interpreter runtime type for CLR type {type}");
            }
            return value;
        }
    }
}
