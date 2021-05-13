using Jither.Imuse.Scripting.Ast;
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
        private readonly string actionName;

        public EventDeclarationExecuter(EventDeclaration declaration) : base(declaration)
        {
            if (declaration.ActionName != null)
            {
                actionName = declaration.ActionName.Name;
            }
            else if (declaration.ActionDeclaration != null)
            {
                actionDeclaration = new ActionDeclarationExecuter(declaration.ActionDeclaration);
            }

            evt = EventDeclaratorExecuter.Build(declaration.Event);
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
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
