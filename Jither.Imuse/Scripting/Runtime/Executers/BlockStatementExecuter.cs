using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class BlockStatementExecuter : StatementExecuter
    {
        private readonly List<StatementExecuter> statements = new();

        public BlockStatementExecuter(BlockStatement block) : base(block)
        {
            foreach (var stmt in block.Body)
            {
                statements.Add(Build(stmt));
            }
        }

        public override RuntimeValue Execute(ExecutionContext context)
        {
            int position = 0;
            while (position < statements.Count)
            {
                var statement = statements[position++];
                switch (statement)
                {
                    case ConditionalJumpStatementExecuter condJump:
                        // WhenNot = true ? Jump if condition is false
                        // WhenNot = false ? Jump if condition is true
                        if (condJump.Test.Execute(context).AsBoolean(condJump.Test) != condJump.WhenNot)
                        {
                            position = condJump.Destination;
                        }
                        continue;
                    case JumpStatementExecuter jump:
                        position = jump.Destination;
                        continue;
                }
                statement.Execute(context);
            }
            return RuntimeValue.Void;
        }
    }
}
