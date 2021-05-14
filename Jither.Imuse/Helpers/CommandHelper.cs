using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Helpers
{
    //public delegate object CommandMethod(object targetObject, object[] arguments);
    public delegate object CommandMethod(object[] arguments);

    public static class CommandHelper
    {
        /// <summary>
        /// Dynamically compiles a <see cref="CommandMethod"/> delegate for invoking arbitrary commands.
        /// </summary>
        /// <summary>
        /// Dynamically compiles a <see cref="CommandMethod"/> delegate for invoking arbitrary commands.
        /// </summary>
        public static CommandMethod CreateCommandMethod(object targetObject, MethodInfo method)
        {
            ParameterExpression arguments = Expression.Parameter(typeof(object[]), "arguments");

            MethodCallExpression call = Expression.Call(
                Expression.Constant(targetObject, method.DeclaringType),
                method,
                CreateParameterExpressions(method, arguments)
            );

            Expression<CommandMethod> lambda;
            if (method.ReturnType == typeof(void))
            {
                // Return null for void return type
                lambda = Expression.Lambda<CommandMethod>(
                    Expression.Block(call, Expression.Constant(null)),
                    arguments
                );
            }
            else
            {
                lambda = Expression.Lambda<CommandMethod>(
                    Expression.Convert(call, typeof(object)),
                    arguments
                );
            }

            return lambda.Compile();
        }

        private static Expression[] CreateParameterExpressions(MethodInfo method, Expression arguments)
        {
            return method
                .GetParameters()
                .Select((p, index) => Expression.Convert(Expression.ArrayIndex(arguments, Expression.Constant(index)), p.ParameterType))
                .ToArray();
        }
    }
}
