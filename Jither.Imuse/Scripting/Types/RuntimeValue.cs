using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Runtime.Executers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Types
{
    public enum RuntimeType
    {
        None,
        Boolean,
        String,
        Integer,

        Action,
        Command,
        Function,

        Null
    }

    public class ImuseCommand
    {
        public string Name { get; }

        public ImuseCommand(string name)
        {
            Name = name;
        }

        public RuntimeValue Execute(List<RuntimeValue> arguments)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class RuntimeValue
    {
        public static readonly NullValue Null = new();

        public RuntimeType Type { get; }

        public RuntimeValue(RuntimeType type)
        {
            Type = type;
        }

        public string AsString(Node node)
        {
            if (Type != RuntimeType.String)
            {
                ErrorHelper.ThrowTypeError(node, $"Expected string but got {Type}");
            }
            return ((StringValue)this).Value;
        }

        public string AsString(Executer executer)
        {
            return AsString(executer.Node);
        }

        public int AsInteger(Node node)
        {
            if (Type != RuntimeType.Integer)
            {
                ErrorHelper.ThrowTypeError(node, $"Expected integer but got {Type}");
            }
            return ((IntegerValue)this).Value;
        }

        public int AsInteger(Executer executer)
        {
            return AsInteger(executer.Node);
        }

        public bool AsBoolean(Node node)
        {
            if (Type != RuntimeType.Boolean)
            {
                ErrorHelper.ThrowTypeError(node, $"Expected boolean but got {Type}");
            }
            return this == BooleanValue.True;
        }

        public bool AsBoolean(Executer executer)
        {
            return AsBoolean(executer.Node);
        }

        public ImuseCommand AsCommand(Node node)
        {
            if (Type != RuntimeType.Command)
            {
                ErrorHelper.ThrowTypeError(node, $"Expected command but got {Type}");
            }
            return ((CommandValue)this).Value;
        }

        public ImuseCommand AsCommand(Executer executer)
        {
            return AsCommand(executer.Node);
        }
    }

    public class BooleanValue : RuntimeValue
    {
        public static readonly BooleanValue True = new(true);
        public static readonly BooleanValue False = new(false);
        public bool Value { get; }

        private BooleanValue(bool value) : base(RuntimeType.Boolean)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value ? "true" : "false";
        }
    }

    public class NullValue : RuntimeValue
    {
        internal NullValue() : base(RuntimeType.Null)
        {

        }

        public override string ToString()
        {
            return "null";
        }
    }

    public class StringValue : RuntimeValue
    {
        public string Value { get; }

        public StringValue(string value) : base(RuntimeType.String)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public class IntegerValue : RuntimeValue
    {
        public int Value { get; }

        public IntegerValue(int value) : base(RuntimeType.Integer)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class ActionValue : RuntimeValue
    {
        public string Name { get; }
        public int? During { get; }
        public StatementExecuter BodyExecuter { get; }

        public ActionValue(string name, int? during, StatementExecuter bodyExecuter) : base(RuntimeType.Action)
        {
            Name = name;
            During = during;
            BodyExecuter = bodyExecuter;
        }

        public override string ToString()
        {
            return $"action:{Name ?? "<anonymous>"}";
        }
    }

    public class CommandValue : RuntimeValue
    {
        public ImuseCommand Value { get; }

        public CommandValue(ImuseCommand value) : base(RuntimeType.Command)
        {
            Value = value;
        }

        public override string ToString()
        {
            return $"command:{Value.Name}";
        }
    }
}
