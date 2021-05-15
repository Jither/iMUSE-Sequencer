using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Events;
using Jither.Imuse.Scripting.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class EventDeclarationExecuter : DeclarationExecuter
    {
        private readonly EventDeclaratorExecuter evt;
        private readonly ActionDeclarationExecuter actionDeclaration;
        private readonly Identifier actionName;

        public EventDeclarationExecuter(EventDeclaration declaration) : base(declaration)
        {
            if (declaration.ActionName != null)
            {
                actionName = declaration.ActionName;
            }
            else if (declaration.ActionDeclaration != null)
            {
                actionDeclaration = new ActionDeclarationExecuter(declaration.ActionDeclaration);
            }

            evt = EventDeclaratorExecuter.Build(declaration.Event);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            ImuseAction action;
            if (actionName != null)
            {
                action = context.CurrentScope.GetSymbol(actionName, actionName.Name).Value.AsAction(actionName);
            }
            else if (actionDeclaration != null)
            {
                action = actionDeclaration.GetValue(context).AsAction(actionDeclaration);
            }
            else
            {
                throw new InvalidOperationException($"Executer found neither action name nor action declaration for event...");
            }

            try
            {
                switch (evt)
                {
                    case StartEventDeclaratorExecuter start:
                        start.Execute(context);
                        context.Events.RegisterEvent(new StartEvent(action));
                        break;
                    case TimeEventDeclaratorExecuter timed:
                        var time = timed.GetValue(context).AsTime(timed);
                        context.Events.RegisterEvent(new TimedEvent(time, action));
                        break;
                    case KeyPressEventDeclaratorExecuter keyPress:
                        var key = keyPress.GetValue(context).AsString(keyPress);
                        context.Events.RegisterEvent(new KeyPressEvent(key, action));
                        break;
                    default:
                        throw new NotImplementedException($"Event registration for {evt} not implemented in executer");
                }
            }
            catch (EventException ex)
            {
                ErrorHelper.ThrowEngineError(this.Node, $"Error registering event: {ex.Message}");
            }

            return ExecutionResult.Void;
        }
    }

    public abstract class EventDeclaratorExecuter : Executer
    {
        protected EventDeclaratorExecuter(Node node) : base(node)
        {
        }

        public static EventDeclaratorExecuter Build(EventDeclarator declarator)
        {
            return declarator switch
            {
                KeyPressEventDeclarator keypress => new KeyPressEventDeclaratorExecuter(keypress),
                TimeEventDeclarator time => new TimeEventDeclaratorExecuter(time),
                StartEventDeclarator start => new StartEventDeclaratorExecuter(start),
                _ => ErrorHelper.ThrowUnknownNode<EventDeclaratorExecuter>(declarator)
            };
        }
    }

    public class KeyPressEventDeclaratorExecuter : EventDeclaratorExecuter
    {
        private readonly ExpressionExecuter key;

        public KeyPressEventDeclaratorExecuter(KeyPressEventDeclarator node) : base(node)
        {
            key = ExpressionExecuter.Build(node.Key);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            string result = key.GetValue(context).AsString(key);

            return new ExecutionResult(ExecutionResultType.Normal, new StringValue(result));
        }
    }

    public class TimeEventDeclaratorExecuter : EventDeclaratorExecuter
    {
        private readonly ExpressionExecuter time;
        private readonly ExpressionExecuter measure;
        private readonly ExpressionExecuter beat;
        private readonly ExpressionExecuter tick;

        public TimeEventDeclaratorExecuter(TimeEventDeclarator node) : base(node)
        {
            if (node.Time != null)
            {
                time = ExpressionExecuter.Build(node.Time);
            }
            if (node.Measure != null)
            {
                measure = ExpressionExecuter.Build(node.Measure);
            }
            if (node.Beat != null)
            {
                beat = ExpressionExecuter.Build(node.Beat);
            }
            if (node.Tick != null)
            {
                tick = ExpressionExecuter.Build(node.Tick);
            }
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            TimeValue result;
            if (time != null)
            {
                var t = time.GetValue(context).AsTime(time);
                result = new TimeValue(t);
            }
            else
            {
                int m = measure?.GetValue(context).AsInteger(measure) ?? 0;
                int b = beat?.GetValue(context).AsInteger(beat) ?? 0;
                int t = tick?.GetValue(context).AsInteger(tick) ?? 0;
                result = new TimeValue(new Time(m, b, t));
            }

            return new ExecutionResult(ExecutionResultType.Normal, result);
        }
    }

    public class StartEventDeclaratorExecuter : EventDeclaratorExecuter
    {
        public StartEventDeclaratorExecuter(StartEventDeclarator node) : base(node)
        {
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            return ExecutionResult.Void;
        }
    }
}
