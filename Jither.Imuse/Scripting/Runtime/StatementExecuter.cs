using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public abstract class StatementExecuter : Executer
    {
        public static StatementExecuter Build(Statement stmt)
        {
            return stmt switch
            {
                AssignmentStatement assign => new AssignmentStatementExecuter(assign),
                BlockStatement block => new BlockStatementExecuter(block),
                CallStatement call => new CallStatementExecuter(call),
                CaseStatement switchCase => new CaseStatementExecuter(switchCase),
                DoStatement doStmt => new DoStatementExecuter(doStmt),
                EnqueueStatement enqueue => new EnqueueStatementExecuter(enqueue),
                ForStatement forStmt => new ForStatementExecuter(forStmt),
                IfStatement ifStmt => new IfStatementExecuter(ifStmt),
                WhileStatement whileStmt => new WhileStatementExecuter(whileStmt),
                _ => ErrorHelper.ThrowUnknownNode<StatementExecuter>(stmt)
            };
        }
    }
}
