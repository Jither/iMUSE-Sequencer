using Jither.Imuse.Commands;
using Jither.Imuse.Helpers;
using Jither.Imuse.Scripting.Events;
using Jither.Imuse.Scripting.Runtime.Executers;
using Jither.Imuse.Scripting.Types;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jither.Imuse.Scripting.Runtime
{
    public class SuspendedAction
    {
        public ImuseAction Action { get; }
        public int Position { get; }
        public Scope LocalScope { get; }
        public int FramesRemaining { get; set; }

        public SuspendedAction(ImuseAction action, int position, Scope localScope, int frameCount)
        {
            Action = action;
            Position = position;
            LocalScope = localScope;
            FramesRemaining = frameCount;
        }
    }

    public class ExecutionContext
    {
        public ImuseEngine Engine { get; }
        public EventManager Events { get; }
        public ImuseQueue Queue { get; }
        public FileProvider FileProvider { get; }
        public Scope CurrentScope => scopes.Peek();
        public EnqueueCommandList EnqueuingCommands { get; set; }

        private readonly Stack<Scope> scopes = new();
        private readonly List<SuspendedAction> suspendedActions = new();

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
            EnterScope(scope);
        }

        public void EnterScope(Scope scope)
        {
            scopes.Push(scope);
        }

        public void ExitScope()
        {
            scopes.Pop();
        }

        public void SuspendAction(ImuseAction action, int position, int frameCount)
        {
            suspendedActions.Add(new SuspendedAction(action, position, CurrentScope, frameCount));
        }

        public void ResumeScripts()
        {
            // Storing the current number of suspended scripts in order to not resume scripts that are re-added after resuming
            int count = suspendedActions.Count;
            int listIndex = 0;
            for (int i = 0; i < count; i++)
            {
                var action = suspendedActions[listIndex];
                action.FramesRemaining--;
                if (action.FramesRemaining == 0)
                {
                    suspendedActions.RemoveAt(listIndex);
                    action.Action.Resume(this, action.Position, action.LocalScope);
                }
                else
                {
                    listIndex++;
                }
            }
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
