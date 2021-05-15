using Jither.Imuse.Helpers;
using Jither.Imuse.Scripting.Events;
using Jither.Imuse.Scripting.Types;
using System;
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
        public ImuseQueue Queue { get; }
        public FileProvider FileProvider { get; }
        public Scope CurrentScope => scopes.Peek();

        private readonly Stack<Scope> scopes = new();

        public ExecutionContext(ImuseEngine engine, CommandManager commands, EventManager events, ImuseQueue queue, FileProvider fileProvider)
        {
            Engine = engine;
            Events = events;
            Commands = commands;
            Queue = queue;
            FileProvider = fileProvider;

            // Intrinsic scope (commands, params etc.)
            scopes.Push(new Scope("Intrinsic", null));
            PopulateCommands(Commands);

            // Global scope (for script)
            scopes.Push(new Scope("Global", CurrentScope));
        }

        public void EnterScope(string name)
        {
            var scope = new Scope(name, CurrentScope);
            scopes.Push(scope);
        }

        public void ExitScope()
        {
            scopes.Pop();
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
                var returnType = RuntimeTypes.FromClrType(method.ReturnType);
                var call = CommandHelper.CreateCommandMethod(commandObject, method);
                var command = new Command(name, commandParameters, returnType, call);
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
