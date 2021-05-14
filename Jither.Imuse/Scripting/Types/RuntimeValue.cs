using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Runtime.Executers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Types
{

    public abstract class RuntimeValue
    {
        public static readonly NullValue Null = new();
        public static readonly VoidValue Void = new();

        public RuntimeType Type { get; }

        public abstract object UntypedValue { get; }

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

        public Command AsCommand(Node node)
        {
            if (Type != RuntimeType.Command)
            {
                ErrorHelper.ThrowTypeError(node, $"Expected command but got {Type}");
            }
            return ((CommandValue)this).Value;
        }

        public Command AsCommand(Executer executer)
        {
            return AsCommand(executer.Node);
        }

        public Time AsTime(Executer executer)
        {
            return AsTime(executer.Node);
        }

        public Time AsTime(Node node)
        {
            if (Type != RuntimeType.Time)
            {
                ErrorHelper.ThrowTypeError(node, $"Expected time but got {Type}");
            }
            return ((TimeValue)this).Value;
        }

        public ImuseAction AsAction(Executer executer)
        {
            return AsAction(executer.Node);
        }

        public ImuseAction AsAction(Node node)
        {
            if (Type != RuntimeType.Action)
            {
                ErrorHelper.ThrowTypeError(node, $"Expected action but got {Type}");
            }
            return ((ActionValue)this).Value;
        }
    }
}
