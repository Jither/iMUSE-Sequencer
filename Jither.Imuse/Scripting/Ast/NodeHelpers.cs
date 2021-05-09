using Jither.Imuse.Scripting.Ast;
using System;

namespace Jither.Imuse.Scripting.Ast
{
    public static class AstExtensions
    {
        public static string OperatorString(this AssignmentOperator op)
        {
            return op switch
            {
                AssignmentOperator.Equals => "=",
                AssignmentOperator.Add => "+=",
                AssignmentOperator.Subtract => "-=",
                AssignmentOperator.Multiply => "*=",
                AssignmentOperator.Divide => "/=",
                _ => throw new NotImplementedException($"{op} not implemented")
            };
        }

        public static string OperatorString(this UnaryOperator op)
        {
            return op switch
            {
                UnaryOperator.Not => "!",
                UnaryOperator.Plus => "+",
                UnaryOperator.Minus => "-",
                _ => throw new NotImplementedException($"{op} not implemented")
            };
        }

        public static string OperatorString(this UpdateOperator op)
        {
            return op switch
            {
                UpdateOperator.Increment => "++",
                UpdateOperator.Decrement => "--",
                _ => throw new NotImplementedException($"{op} not implemented")
            };
        }

        public static string OperatorString(this BinaryOperator op)
        {
            return op switch
            {
                BinaryOperator.Add => "+",
                BinaryOperator.And => "&&",
                BinaryOperator.Divide => "/",
                BinaryOperator.Equal => "==",
                BinaryOperator.Greater => ">",
                BinaryOperator.GreaterOrEqual => ">=",
                BinaryOperator.Less => "<",
                BinaryOperator.LessOrEqual => "<=",
                BinaryOperator.Modulo => "%",
                BinaryOperator.Multiply => "*",
                BinaryOperator.NotEqual => "!=",
                BinaryOperator.Or => "||",
                BinaryOperator.Subtract => "-",
                _ => throw new NotImplementedException($"{op} not implemented")
            };
        }
    }
}
