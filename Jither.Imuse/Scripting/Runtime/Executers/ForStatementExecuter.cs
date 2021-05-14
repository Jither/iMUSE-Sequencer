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
        private readonly bool? increment;

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

            var counter = context.CurrentScope.AddOrUpdateSymbol(this.iterator, iteratorName, start);
            // Don't allow mutating the iterator during the loop (unlike C)
            counter.IsImmutable = true;

            // Yeah, we actually precalculate and keep our own copy of the counter start/end values.
            // In a C-like language, we wouldn't, because we can't assume the programmer
            // won't change them mid-loop. Here, they can't be - and the counter itself is immutable
            int counterValue = start.AsInteger(from);
            int endValue = end.AsInteger(to);

            // If developer didn't specify increment/decrement, base it on the from/to values:
            bool increment = this.increment ?? counterValue <= endValue;

            if (increment)
            {
                while (counterValue <= endValue)
                {
                    var result = body.Execute(context);
                    if (result.Type == ExecutionResultType.Break)
                    {
                        break;
                    }
                    // Semantic: we'll exit the loop with counter == to
                    if (counterValue == endValue)
                    {
                        break;
                    }

                    counterValue++;
                    counter.UpdateWithNoChecks(IntegerValue.Create(counterValue));
                }
            }
            else
            {
                while (counterValue >= endValue)
                {
                    var result = body.Execute(context);
                    if (result.Type == ExecutionResultType.Break)
                    {
                        break;
                    }
                    // Semantic: we'll exit the loop with counter == to
                    if (counterValue == endValue)
                    {
                        break;
                    }

                    counterValue--;
                    counter.UpdateWithNoChecks(IntegerValue.Create(counterValue));
                }
            }

            counter.IsImmutable = false;

            return ExecutionResult.Void;
        }
    }
}
