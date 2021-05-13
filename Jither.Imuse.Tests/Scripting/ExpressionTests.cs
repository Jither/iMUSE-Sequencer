using Jither.Imuse.Scripting.Ast;
using Jither.Imuse.Scripting.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jither.Imuse.Scripting
{
    public class ExpressionTests
    {
        [Theory]
        [InlineData("a + b", BinaryOperator.Add, "a", "b")]
        [InlineData("value - foo", BinaryOperator.Subtract, "value", "foo")]
        [InlineData("this-is-a-test * not-a-drill", BinaryOperator.Multiply, "this-is-a-test", "not-a-drill")]
        [InlineData("a / b", BinaryOperator.Divide, "a", "b")]
        [InlineData("a % b", BinaryOperator.Modulo, "a", "b")]
        [InlineData("a && b", BinaryOperator.And, "a", "b")]
        [InlineData("a || b", BinaryOperator.Or, "a", "b")]
        [InlineData("a == b", BinaryOperator.Equal, "a", "b")]
        [InlineData("a != b", BinaryOperator.NotEqual, "a", "b")]
        [InlineData("a > b", BinaryOperator.Greater, "a", "b")]
        [InlineData("a < b", BinaryOperator.Less, "a", "b")]
        [InlineData("a >= b", BinaryOperator.GreaterOrEqual, "a", "b")]
        [InlineData("a <= b", BinaryOperator.LessOrEqual, "a", "b")]
        [InlineData("a and b", BinaryOperator.And, "a", "b")]
        [InlineData("a or b", BinaryOperator.Or, "a", "b")]
        [InlineData("a is b", BinaryOperator.Equal, "a", "b")]
        [InlineData("a is-not b", BinaryOperator.NotEqual, "a", "b")]
        public void Parses_simple_binary_expressions(string source, BinaryOperator expectedOperator, string expectedLeftName, string expectedRightName)
        {
            var parser = new ScriptParser(source);
            var expr = parser.ParseExpression();

            var binExpr = Assert.IsType<BinaryExpression>(expr);

            Assert.Equal(expectedOperator, binExpr.Operator);
            var left = Assert.IsType<Identifier>(binExpr.Left);
            var right = Assert.IsType<Identifier>(binExpr.Right);
            Assert.Equal(expectedLeftName, left.Name);
            Assert.Equal(expectedRightName, right.Name);
        }

        [Fact]
        public void Handles_simple_precedence()
        {
            var parser = new ScriptParser("a + b * c - d");
            var expr = parser.ParseExpression();

            ScriptAssert.Nodes(expr,
                new TestBinary(
                    new TestBinary(
                        new TestIdentifier("a"),
                        BinaryOperator.Add,
                        new TestBinary(
                            new TestIdentifier("b"),
                            BinaryOperator.Multiply,
                            new TestIdentifier("c")
                        )
                    ),
                    BinaryOperator.Subtract,
                    new TestIdentifier("d")
                )
            );
        }

        [Fact]
        public void Handles_parenthesis_precedence()
        {
            var parser = new ScriptParser("a + b * (c - d)");
            var expr = parser.ParseExpression();

            ScriptAssert.Nodes(expr,
                new TestBinary(
                    new TestIdentifier("a"),
                    BinaryOperator.Add,
                    new TestBinary(
                        new TestIdentifier("b"),
                        BinaryOperator.Multiply,
                        new TestBinary(
                            new TestIdentifier("c"),
                            BinaryOperator.Subtract,
                            new TestIdentifier("d")
                        )
                    )
                )
            );
        }

        [Fact]
        public void Handles_nested_parenthesis_precedence()
        {
            var parser = new ScriptParser("a + b * (c / (d - e))");
            var expr = parser.ParseExpression();

            ScriptAssert.Nodes(expr,
                new TestBinary(
                    new TestIdentifier("a"),
                    BinaryOperator.Add,
                    new TestBinary(
                        new TestIdentifier("b"),
                        BinaryOperator.Multiply,
                        new TestBinary(
                            new TestIdentifier("c"),
                            BinaryOperator.Divide,
                            new TestBinary(
                                new TestIdentifier("d"),
                                BinaryOperator.Subtract,
                                new TestIdentifier("e")
                            )
                        )
                    )
                )
            );
        }

        [Fact]
        public void Handles_simple_unary()
        {
            var parser = new ScriptParser("-a");
            var expr = parser.ParseExpression();

            ScriptAssert.Nodes(expr, new TestUnary(UnaryOperator.Minus, new TestIdentifier("a")));
        }

        [Fact]
        public void Handles_simple_unary2()
        {
            var parser = new ScriptParser("not b");
            var expr = parser.ParseExpression();

            ScriptAssert.Nodes(expr, new TestUnary(UnaryOperator.Not, new TestIdentifier("b")));
        }

        [Fact]
        public void Handles_simple_update_prefix()
        {
            var parser = new ScriptParser("--b");
            var expr = parser.ParseExpression();

            ScriptAssert.Nodes(expr, new TestUpdate(UpdateOperator.Decrement, new TestIdentifier("b")));
        }

        [Fact]
        public void Handles_simple_update_prefix2()
        {
            var parser = new ScriptParser("++b");
            var expr = parser.ParseExpression();

            ScriptAssert.Nodes(expr, new TestUpdate(UpdateOperator.Increment, new TestIdentifier("b")));
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("5", 5)]
        [InlineData("20", 20)]
        [InlineData("2147483647", 2147483647)]
        public void Parses_integer_literals(string source, int expected)
        {
            var parser = new ScriptParser(source);
            var expr = parser.ParseExpression();

            var literal = Assert.IsType<Literal>(expr);
            Assert.Equal(LiteralType.Integer, literal.ValueType);
            Assert.Equal(expected, literal.IntegerValue);
        }

        [Theory]
        [InlineData("4.2.400", 4, 2, 400)]
        [InlineData("5.100", 0, 5, 100)]
        [InlineData("0.5.200", 0, 5, 200)]
        public void Parses_time_literals(string source, int expectedMeasure, int expectedBeat, int expectedTick)
        {
            var parser = new ScriptParser(source);
            var expr = parser.ParseExpression();

            var expected = new Time(expectedMeasure, expectedBeat, expectedTick);

            var literal = Assert.IsType<Literal>(expr);
            Assert.Equal(LiteralType.Time, literal.ValueType);
            Assert.Equal(expected, literal.TimeValue);
        }

        [Theory]
        [InlineData("'three-headed monkey'", "three-headed monkey")]
        [InlineData(@"'The Secret of\nMonkey Island'", "The Secret of\nMonkey Island")]
        public void Parses_string_literals(string source, string expected)
        {
            var parser = new ScriptParser(source);
            var expr = parser.ParseExpression();

            var literal = Assert.IsType<Literal>(expr);
            Assert.Equal(LiteralType.String, literal.ValueType);
            Assert.Equal(expected, literal.StringValue);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void Parses_boolean_literals(string source, bool expected)
        {
            var parser = new ScriptParser(source);
            var expr = parser.ParseExpression();

            var literal = Assert.IsType<Literal>(expr);
            Assert.Equal(LiteralType.Boolean, literal.ValueType);
            Assert.Equal(expected, literal.BooleanValue);
        }

    }
}
