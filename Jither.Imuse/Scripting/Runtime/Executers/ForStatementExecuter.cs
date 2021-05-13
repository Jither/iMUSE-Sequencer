using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public class ForStatementExecuter : StatementExecuter
    {
        private readonly Identifier iterator;
        private readonly string iteratorName;
        private readonly ExpressionExecuter from;
        private readonly ExpressionExecuter to;
        private readonly StatementExecuter body;
        private readonly bool increment;

        public ForStatementExecuter(ForStatement stmt) : base(stmt)
        {
            iterator = stmt.Iterator;
            iteratorName = stmt.Iterator.Name;
            from = ExpressionExecuter.Build(stmt.From);
            to = ExpressionExecuter.Build(stmt.To);
            body = Build(stmt.Body);
            increment = stmt.Increment;
        }

        public override ExecutionResult Execute(ExecutionContext context)
        {
            var start = from.GetValue(context);
            var end = to.GetValue(context);

            var iterator = context.CurrentScope.AddOrUpdateSymbol(this.iterator, iteratorName, start);

            // Yeah, we actually precalculate and keep our own copy of the iterator start/end values.
            // In a C-like language, we wouldn't, because we can't assume the programmer
            // won't change them mid-loop. Here, we're making the rule that
            // you can do that, and it will have your value until the next iteration, when
            // it will stubbornly change back to the intended value.
            int iteratorValue = start.AsInteger(from);
            int endValue = end.AsInteger(to);

            if (increment)
            {
                while (iteratorValue <= endValue)
                {
                    var result = body.Execute(context);
                    if (result.Type == ExecutionResultType.Break)
                    {
                        break;
                    }
                    iteratorValue++;
                    iterator.Update(Node, new IntegerValue(iteratorValue));
                }
            }
            else
            {
                while (iteratorValue >= endValue)
                {
                    var result = body.Execute(context);
                    if (result.Type == ExecutionResultType.Break)
                    {
                        break;
                    }
                    iteratorValue--;
                    iterator.Update(Node, new IntegerValue(iteratorValue));
                }
            }

            return ExecutionResult.Void;
        }
    }
}
