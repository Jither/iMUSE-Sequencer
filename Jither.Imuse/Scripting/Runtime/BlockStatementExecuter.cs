using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting.Runtime
{
    public class BlockStatementExecuter : StatementExecuter
    {
        private readonly List<StatementExecuter> statements = new List<StatementExecuter>();

        public BlockStatementExecuter(BlockStatement block)
        {
            foreach (var stmt in block.Body)
            {
                statements.Add(Build(stmt));
            }
        }

        protected override ExecutionResult Execute(ExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }

}
