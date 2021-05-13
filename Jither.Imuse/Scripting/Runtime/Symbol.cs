using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime
{
    public class Symbol
    {
        public string Name { get; }
        public RuntimeValue Value { get; private set; }
        public bool IsConstant { get; }

        public Symbol(string name, RuntimeValue value, bool isConstant = false)
        {
            Name = name;
            Value = value;
            IsConstant = isConstant;
        }

        public void Update(Node node, RuntimeValue value)
        {
            if (IsConstant)
            {
                ErrorHelper.ThrowTypeError(node, $"Cannot assign {value} to {Name} - it's a constant");
            }
            Value = value;
        }

        public override string ToString()
        {
            if (IsConstant)
            {
                return $"const {Name,-30}: {Value}";
            }
            return $"      {Name,-30}: {Value}";
        }
    }
}
