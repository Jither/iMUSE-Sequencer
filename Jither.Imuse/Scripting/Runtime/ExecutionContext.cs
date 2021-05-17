using Jither.Imuse.Commands;
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
        public ImuseQueue Queue { get; }
        public FileProvider FileProvider { get; }
        public Scope CurrentScope => scopes.Peek();
        public List<CommandCall> EnqueuingCommands { get; set; }

        private readonly Stack<Scope> scopes = new();

        public ExecutionContext(ImuseEngine engine, EventManager events, ImuseQueue queue, FileProvider fileProvider)
        {
            Engine = engine;
            Events = events;
            Queue = queue;
            FileProvider = fileProvider;

            // Intrinsic scope (commands, params etc.)
            scopes.Push(new Scope("Intrinsic", null));
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

        public void AddCommands(object commandObject)
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
                
                var enqueuable = method.GetCustomAttribute<EnqueueableAttribute>() != null;

                // Enqueuing a function call (return value) is a mistake. Functions shouldn't be queueable, since the queue doesn't handle it -
                // this could e.g. be a call to random(), which should be executed immediately, even in the enqueuing state.
                if (enqueuable && returnType != RuntimeType.Void)
                {
                    throw new InvalidOperationException($"Commands with return value should not be enqueuable. {method.DeclaringType.Name}.{method.Name} returns {method.ReturnType}");
                }

                var command = new Command(name, commandParameters, returnType, call, enqueuable);
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
