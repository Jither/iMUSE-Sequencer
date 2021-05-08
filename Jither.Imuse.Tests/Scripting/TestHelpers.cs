using Jither.Imuse.Scripting.Ast;
using System.Text.RegularExpressions;
using Xunit;

namespace Jither.Imuse.Scripting
{
    public static class StringExtensions
    {
        public static string NormalizeNewLines(this string str)
        {
            return str.Replace("\r\n", "\n");
        }
    }

    public class NodeAssertion<T> where T : Node
    {

    }

    public partial class ScriptAssert
    {
        private static readonly Regex rxNewLine = new(@"\r?\n");

        private static string[] SplitIntoLines(string source)
        {
            return rxNewLine.Split(source);
        }

        private static string ExtractText(string source, Node node)
        {
            int startIndex = 0;
            for (int i = 0; i < node.Range.Start.Line; i++)
            {
                startIndex = source.IndexOf('\n', startIndex) + 1;
            }
            startIndex += node.Range.Start.Column;

            int endIndex = 0;
            for (int i = 0; i < node.Range.End.Line; i++)
            {
                endIndex = source.IndexOf('\n', endIndex) + 1;
            }
            endIndex += node.Range.End.Column;

            return source[startIndex..endIndex];
        }

        public static void NodeMatches(string source, string nodeText, NodeType nodeType, Node actual)
        {
            Assert.Equal(nodeType, actual.Type);
            // Check start/end line/column
            var actualNodeText = ExtractText(source, actual);
            Assert.Equal(nodeText, actualNodeText);
            // Check start/end index
            actualNodeText = source[actual.Range.Start.Index..actual.Range.End.Index];
            Assert.Equal(nodeText, actualNodeText);
        }

        public static void Nodes<T>(Node actual, TestNode<T> match) where T : Node
        {
            var node = Assert.IsType<T>(actual);
            match.Check(node);
        }
    }

    public abstract class TestNode
    {
        public abstract void Check(Node node);
    }

    public abstract class TestNode<T> : TestNode where T : Node
    {
        public abstract void Check(T node);

        public override void Check(Node node)
        {
            var typedNode = Assert.IsType<T>(node);
            Check(typedNode);
        }
    }

    public class TestIdentifier : TestNode<Identifier>
    {
        private readonly string name;

        public TestIdentifier(string name)
        {
            this.name = name;
        }

        public override void Check(Identifier node)
        {
            Assert.Equal(name, node.Name);
        }
    }

    public class TestUnary : TestNode<UnaryExpression>
    {
        private readonly TestNode argumentNode;
        private readonly UnaryOperator op;

        public TestUnary(UnaryOperator op, TestNode argument)
        {
            argumentNode = argument;
            this.op = op;
        }

        public override void Check(UnaryExpression node)
        {
            Assert.Equal(op, node.Operator);
            argumentNode.Check(node.Argument);
        }
    }

    public class TestUpdate : TestNode<UpdateExpression>
    {
        private readonly TestNode argumentNode;
        private readonly UpdateOperator op;

        public TestUpdate(UpdateOperator op, TestNode argument)
        {
            argumentNode = argument;
            this.op = op;
        }

        public override void Check(UpdateExpression node)
        {
            Assert.Equal(op, node.Operator);
            argumentNode.Check(node.Argument);
        }
    }

    public class TestBinary : TestNode<BinaryExpression>
    {
        private readonly TestNode left;
        private readonly TestNode right;
        private readonly BinaryOperator op;

        public TestBinary(TestNode left, BinaryOperator op, TestNode right)
        {
            this.left = left;
            this.right = right;
            this.op = op;
        }

        public override void Check(BinaryExpression actual)
        {
            Assert.Equal(op, actual.Operator);
            left.Check(actual.Left);
            right.Check(actual.Right);
        }
    }
}
