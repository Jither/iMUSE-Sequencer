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
        public bool WhenNot { get; }

        public ConditionalJumpStatementExecuter(ConditionalJumpStatement statement) : base(statement)
        {
            Test = ExpressionExecuter.Build(statement.Test);
            WhenNot = statement.WhenNot;
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
                BlockStatement block => new BlockStatementExecuter(block),
                //BreakStatement breakStmt => new BreakStatementExecuter(breakStmt),
                //CaseStatement switchCase => new CaseStatementExecuter(switchCase),
                //DoStatement doStmt => new DoStatementExecuter(doStmt),
                //EnqueueStatement enqueue => new EnqueueStatementExecuter(enqueue),
                ExpressionStatement expr => new ExpressionStatementExecuter(expr),
                //ForStatement forStmt => new ForStatementExecuter(forStmt),
                //IfStatement ifStmt => new IfStatementExecuter(ifStmt),
                //WhileStatement whileStmt => new WhileStatementExecuter(whileStmt),

                ConditionalJumpStatement condJump => new ConditionalJumpStatementExecuter(condJump),
                JumpStatement jump => new JumpStatementExecuter(jump),
                
                EnqueueStartStatement enqueueStart => new EnqueueStartStatementExecuter(enqueueStart),
                EnqueueEndStatement enqueueEnd => new EnqueueEndStatementExecuter(enqueueEnd),
                _ => ErrorHelper.ThrowUnknownNode<StatementExecuter>(stmt)
            };
        }
    }
}
