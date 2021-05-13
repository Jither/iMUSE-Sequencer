using Jither.Imuse.Scripting.Ast;
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

        public override ExecutionResult Execute(ExecutionContext context)
        {
            foreach (var stmt in statements)
            {
                var result = stmt.Execute(context);
                if (result.Type == ExecutionResultType.Break)
                {
                    return ExecutionResult.Break;
                }
            }
            return ExecutionResult.Void;
        }
    }
}
