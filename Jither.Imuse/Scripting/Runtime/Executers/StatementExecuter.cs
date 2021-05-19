using Jither.Imuse.Commands;
using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System.Collections;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class EnqueueCommandList : IEnumerable<CommandCall>
    {
        public int SoundId { get; }
        public int MarkerId { get; }

        private readonly List<CommandCall> commands = new();

        public EnqueueCommandList(int soundId, int markerId)
        {
            SoundId = soundId;
            MarkerId = markerId;
        }

        public void Add(CommandCall command)
        {
            commands.Add(command);
        }

        public IEnumerator<CommandCall> GetEnumerator()
        {
            return commands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class JumpStatementExecuter : StatementExecuter
    {
        public int Destination { get; }

        public JumpStatementExecuter(JumpStatement statement) : base(statement)
        {
            Destination = statement.Destination;
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            // Should be handled from outside
            throw new System.NotImplementedException();
        }
    }

    public class ConditionalJumpStatementExecuter : JumpStatementExecuter
    {
        public ExpressionExecuter Test { get; }
        public bool JumpWhen { get; }

        public ConditionalJumpStatementExecuter(ConditionalJumpStatement statement) : base(statement)
        {
            Test = ExpressionExecuter.Build(statement.Test);
            JumpWhen = statement.JumpWhen;
        }
    }

    public abstract class StatementExecuter : Executer
    {
        protected StatementExecuter(Statement stmt) : base(stmt)
        {

        }

        public static StatementExecuter Build(Statement stmt)
        {
            return stmt switch
            {
                ExpressionStatement expr => new ExpressionStatementExecuter(expr),
                BreakHereStatement breakHere => new BreakHereStatementExecuter(breakHere),

                ConditionalJumpStatement condJump => new ConditionalJumpStatementExecuter(condJump),
                JumpStatement jump => new JumpStatementExecuter(jump),
                
                EnqueueStartStatement enqueueStart => new EnqueueStartStatementExecuter(enqueueStart),
                EnqueueEndStatement enqueueEnd => new EnqueueEndStatementExecuter(enqueueEnd),

                _ => ErrorHelper.ThrowUnknownNode<StatementExecuter>(stmt)
            };
        }
    }
}
