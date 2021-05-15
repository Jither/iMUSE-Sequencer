using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Jither.Imuse.Scripting.Types
{
    public enum RuntimeType
    {
        [Display(Name = "void")]
        Void,
        [Display(Name = "bool")]
        Boolean,
        [Display(Name = "string")]
        String,
        [Display(Name = "int")]
        Integer,

        [Display(Name = "action")]
        Action,
        [Display(Name = "command")]
        Command,
        [Display(Name = "time")]
        Time,

        [Display(Name = "null")]
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
