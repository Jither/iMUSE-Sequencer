using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Runtime.Executers;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Types
{
    public class ImuseAction
    {
        public string Name { get; }
        public int? During { get; }
        private readonly List<StatementExecuter> body;

        public ImuseAction(string name, int? during, List<StatementExecuter> body)
        {
            Name = name;
            During = during;
            this.body = body;
        }

        internal void Execute(ExecutionContext context)
        {
            context.EnterScope($"Action {Name}");
            InternalExecute(context, position: 0);
            context.ExitScope();
        }

        internal void Resume(ExecutionContext context, int position, Scope scope)
        {
            context.EnterScope(scope);
            InternalExecute(context, position);
            context.ExitScope();
        }

        private void InternalExecute(ExecutionContext context, int position)
        {
            while (position < body.Count)
            {
                var statement = body[position++];
                switch (statement)
                {
                    case ConditionalJumpStatementExecuter condJump:
                        // JumpWhen == true ? Jump if condition is true
                        // JumpWhen == false ? Jump if condition is false
                        if (condJump.Test.Execute(context).AsBoolean(condJump.Test) == condJump.JumpWhen)
                        {
                            position = condJump.Destination;
                        }
                        continue;
                    case JumpStatementExecuter jump:
                        position = jump.Destination;
                        continue;
                    case BreakHereStatementExecuter breakHere:
                        int count = 1;
                        if (breakHere.Count != null)
                        {
                            count = breakHere.Count.Execute(context).AsInteger(breakHere);
                        }
                        context.SuspendAction(this, position, count);
                        return;
                }
                statement.Execute(context);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
