using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using Jither.Logging;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime
{
    public class Scope
    {
        private static readonly Logger logger = LogProvider.Get(nameof(Scope));

        private readonly Dictionary<string, Symbol> symbols = new();
        private readonly Scope parent;

        public string Name { get; }

        public Scope(string name, Scope parent)
        {
            Name = name;
            this.parent = parent;
        }

        public Symbol AddSymbol(string name, RuntimeValue value, bool isConstant = false)
        {
            var symbol = new Symbol(name, value, isConstant);
            if (symbols.ContainsKey(name))
            {
                throw new InvalidOperationException($"Symbol '{name}' is already defined in this scope");
            }
            symbols.Add(name, symbol);
            return symbol;
        }

        public Symbol AddOrUpdateSymbol(Node node, string name, RuntimeValue value)
        {
            var symbol = TryGetLocalSymbol(name);
            if (symbol != null)
            {
                symbol.Update(node, value);
            }
            else
            {
                symbol = AddSymbol(name, value);
            }
            return symbol;
        }

        public Symbol GetSymbol(Node node, string name)
        {
            var result = TryGetSymbol(node, name);
            if (result == null)
            {
                ErrorHelper.ThrowTypeError(node, $"${name} is not defined.");
            }
            return result;
        }

        public Symbol TryGetSymbol(Node node, string name)
        {
            Scope lookupScope = this;
            while (lookupScope != null)
            {
                Symbol result = lookupScope.TryGetLocalSymbol(name);
                if (result != null)
                {
                    return result;
                }
                lookupScope = lookupScope.parent;
            }
            return null;
        }

        public Symbol TryGetLocalSymbol(string name)
        {
            if (symbols.TryGetValue(name, out Symbol result))
            {
                return result;
            }
            return null;
        }

        public void Dump()
        {
            logger.Info($"Scope: {Name}");
            foreach (var symbol in symbols)
            {
                logger.Info($"  {symbol.Value}");
            }
        }
    }
}
