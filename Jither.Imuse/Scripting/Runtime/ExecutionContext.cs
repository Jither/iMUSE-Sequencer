using Jither.Imuse.Helpers;
using Jither.Imuse.Scripting.Types;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jither.Imuse.Scripting.Runtime
{
    public class ExecutionContext
    {
        public ImuseEngine Engine { get; }
        public EventManager Events { get; }
        public CommandManager Commands { get; }
        public Scope CurrentScope => scopes.Peek();

        private readonly Stack<Scope> scopes = new();

        public ExecutionContext(ImuseEngine engine)
        {
            Engine = engine;
            Events = engine.Events;
            Commands = engine.Commands;

            // Intrinsic scope (commands, params etc.)
            scopes.Push(new Scope("Intrinsic", null));
            PopulateCommands(Commands);

            // Global scope (for script)
            scopes.Push(new Scope("Global", CurrentScope));
        }

        public void PopulateCommands(object commandObject)
        {
            var methods = commandObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<NoScriptingAttribute>() == null);

            foreach (var method in methods)
            {
                var name = method.Name.Scummify();
                var prms = method.GetParameters();
                var commandParameters = new List<CommandParameter>();
                foreach (var paramInfo in prms)
                {
                    var paramName = paramInfo.Name.Scummify();
                    var paramType = RuntimeTypes.FromClrType(paramInfo.ParameterType);
                    commandParameters.Add(new CommandParameter(paramName, paramType));
                }
                var command = new Command(name, commandParameters);
                CurrentScope.AddSymbol(name, new CommandValue(command), isConstant: true);
            }
        }

        public void Dump()
        {
            foreach (var scope in scopes)
            {
                scope.Dump();
            }
        }
    }
}
