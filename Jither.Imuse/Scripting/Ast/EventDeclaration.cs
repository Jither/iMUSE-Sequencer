using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Ast
{
    public class EventDeclaration : Declaration
    {
        public override NodeType Type => NodeType.EventDeclaration;

        public EventDeclarator Event { get; }
        public ActionDeclaration ActionDeclaration { get; }
        public Identifier ActionName { get; }

        public EventDeclaration(EventDeclarator evt, ActionDeclaration action)
        {
            Event = evt;
            ActionDeclaration = action;
        }

        public EventDeclaration(EventDeclarator evt, Identifier action)
        {
            Event = evt;
            ActionName = action;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Event;
                if (ActionDeclaration != null)
                {
                    yield return ActionDeclaration;
                }
                if (ActionName != null)
                {
                    yield return ActionName;
                }
            }
        }
        public override void Accept(IAstVisitor visitor) => visitor.VisitEventDeclaration(this);
    }

    public abstract class EventDeclarator : Node
    {
    }

    public class KeyPressEventDeclarator : EventDeclarator
    {
        public override NodeType Type => NodeType.KeyPressEventDeclarator;
        public Expression Key { get; }

        public KeyPressEventDeclarator(Expression key)
        {
            Key = key;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                yield return Key;
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitKeyPressEventDeclarator(this);
    }

    public class TimeEventDeclarator : EventDeclarator
    {
        public override NodeType Type => NodeType.TimeEventDeclarator;

        public Expression Time { get; }

        public Expression Measure { get; }
        public Expression Beat { get; }
        public Expression Tick { get; }

        public TimeEventDeclarator(Expression measure, Expression beat, Expression tick)
        {
            Measure = measure;
            Beat = beat;
            Tick = tick;
        }

        public TimeEventDeclarator(Expression time)
        {
            Time = time;
        }

        public override IEnumerable<Node> Children
        {
            get
            {
                if (Time != null)
                {
                    yield return Time;
                }

                if (Measure != null)
                {
                    yield return Measure;
                }
                if (Beat != null)
                {
                    yield return Beat;
                }
                if (Tick != null)
                {
                    yield return Tick;
                }
            }
        }

        public override void Accept(IAstVisitor visitor) => visitor.VisitTimeEventDeclarator(this);
    }

    public class StartEventDeclarator : EventDeclarator
    {
        public StartEventDeclarator()
        {
        }

        public override NodeType Type => NodeType.StartEventDeclarator;

        public override IEnumerable<Node> Children => Enumerable.Empty<Node>();

        public override void Accept(IAstVisitor visitor) => visitor.VisitStartEventDeclarator(this);

    }
}
