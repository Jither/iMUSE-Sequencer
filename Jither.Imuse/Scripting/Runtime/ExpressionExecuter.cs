using Jither.Imuse.Scripting.Ast;

namespace Jither.Imuse.Scripting.Runtime
{
    public abstract class ExpressionExecuter : Executer
    {
        public static ExpressionExecuter Build(Expression expression)
        {
            return expression switch
            {
                BinaryExpression binary => new BinaryExpressionExecuter(binary),
                UpdateExpression update => new UpdateExpressionExecuter(update),
                UnaryExpression unary => new UnaryExpressionExecuter(unary),
                Identifier identifier => new IdentifierExecuter(identifier),
                FunctionCallExpression call => new FunctionCallExpressionExecuter(call),
                Literal literal => new LiteralExecuter(literal),
                _ => ErrorHelper.ThrowUnknownNode<ExpressionExecuter>(expression),
            };
        }
    }
}
