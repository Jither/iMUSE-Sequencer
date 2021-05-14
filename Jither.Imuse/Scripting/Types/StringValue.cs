using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Runtime.Executers;
using System.Text.RegularExpressions;

namespace Jither.Imuse.Scripting.Types
{
    public class StringValue : RuntimeValue
    {
        private static readonly Regex FormatToken = new Regex("%(?<id>.*?)%", RegexOptions.Compiled);
        public string Value { get; }
        public override object UntypedValue => Value;

        private bool mayNeedFormatting = false;

        public StringValue(string value) : base(RuntimeType.String)
        {
            Value = value;
            mayNeedFormatting = value.Contains('%');
        }

        public StringValue Format(Executer executer, ExecutionContext context)
        {
            // If there's no possibility of format tokens in this string, just return original
            if (!mayNeedFormatting)
            {
                return this;
            }

            string result = FormatToken.Replace(Value, match =>
            {
                var identifier = match.Groups["id"].Value;
                var symbol = context.CurrentScope.TryGetSymbol(executer.Node, identifier);
                return symbol?.Value.ToString() ?? match.Value;
            });

            // If no change, just return original string
            if (result == Value)
            {
                return this;
            }
            return new StringValue(result);
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool IsEqualTo(RuntimeValue other)
        {
            return other is StringValue str && str.Value == Value;
        }
    }
}
