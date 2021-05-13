using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime.Executers
{
    public abstract class ExpressionExecuter : Executer
    {
        protected ExpressionExecuter(Expression expr) : base(expr)
        {

        }

        public static ExpressionExecuter Build(Expression expression)
        {
            return expression switch
            {
                AssignmentExpression assign => new AssignmentExpressionExecuter(assign),
                BinaryExpression binary => new BinaryExpressionExecuter(binary),
                UpdateExpression update => new UpdateExpressionExecuter(update),
                UnaryExpression unary => new UnaryExpressionExecuter(unary),
                Identifier identifier => new IdentifierExecuter(identifier),
                CallExpression call => new CallExpressionExecuter(call),
                Literal literal => new LiteralExecuter(literal),
                _ => ErrorHelper.ThrowUnknownNode<ExpressionExecuter>(expression),
            };
        }
    }
}
