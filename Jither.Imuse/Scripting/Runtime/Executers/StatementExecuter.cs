using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
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
                BreakStatement breakStmt => new BreakStatementExecuter(breakStmt),
                CaseStatement switchCase => new CaseStatementExecuter(switchCase),
                DoStatement doStmt => new DoStatementExecuter(doStmt),
                EnqueueStatement enqueue => new EnqueueStatementExecuter(enqueue),
                ExpressionStatement expr => new ExpressionStatementExecuter(expr),
                ForStatement forStmt => new ForStatementExecuter(forStmt),
                IfStatement ifStmt => new IfStatementExecuter(ifStmt),
                WhileStatement whileStmt => new WhileStatementExecuter(whileStmt),
                _ => ErrorHelper.ThrowUnknownNode<StatementExecuter>(stmt)
            };
        }
    }
}
