﻿using Jither.Imuse.Scripting.Ast;
using System.Collections.Generic;
using Xunit;

namespace Jither.Imuse.Scripting
{
    public class FlatAstVisitor : SimpleAstVisitor
    {
        public List<Node> Nodes { get; } = new List<Node>();

        protected override void Visit(Node node)
        {
            Nodes.Add(node);
        }
    }

    public class ParserTests
    {
        [Fact]
        public void Assigns_correct_node_locations_define()
        {
            string source = "define amount-of-wood-a-woodchuck-would-chuck-if-a-woodchuck-could-chuck-wood = 0";
            var parser = new ScriptParser(source);
            var script = parser.Parse();

            var visitor = new FlatAstVisitor();
            visitor.Traverse(script);

            Assert.Collection(visitor.Nodes,
                decl => ScriptAssert.NodeMatches(source, "define amount-of-wood-a-woodchuck-would-chuck-if-a-woodchuck-could-chuck-wood = 0", NodeType.Script, decl),
                decl => ScriptAssert.NodeMatches(source, "define amount-of-wood-a-woodchuck-would-chuck-if-a-woodchuck-could-chuck-wood = 0", NodeType.DefineDeclaration, decl),
                decl => ScriptAssert.NodeMatches(source, "amount-of-wood-a-woodchuck-would-chuck-if-a-woodchuck-could-chuck-wood", NodeType.Identifier, decl),
                decl => ScriptAssert.NodeMatches(source, "0", NodeType.Literal, decl)
            );
        }

        [Fact]
        public void Assigns_correct_node_locations_sounds()
        {
            string source = @"sounds {
    'lechuck' lechuck
    'woodchuck' woodchuck
}";
            var parser = new ScriptParser(source);
            var script = parser.Parse();

            var visitor = new FlatAstVisitor();
            visitor.Traverse(script);

            Assert.Collection(visitor.Nodes,
                decl => ScriptAssert.NodeMatches(source, source, NodeType.Script, decl),
                decl => ScriptAssert.NodeMatches(source, source, NodeType.SoundsDeclaration, decl),
                decl => ScriptAssert.NodeMatches(source, "'lechuck' lechuck", NodeType.SoundDeclarator, decl),
                decl => ScriptAssert.NodeMatches(source, "'lechuck'", NodeType.Literal, decl),
                decl => ScriptAssert.NodeMatches(source, "lechuck", NodeType.Identifier, decl),
                decl => ScriptAssert.NodeMatches(source, "'woodchuck' woodchuck", NodeType.SoundDeclarator, decl),
                decl => ScriptAssert.NodeMatches(source, "'woodchuck'", NodeType.Literal, decl),
                decl => ScriptAssert.NodeMatches(source, "woodchuck", NodeType.Identifier, decl)
            );
        }

        [Fact]
        public void Assigns_correct_node_locations_action()
        {
            string source = @"action my-action during woodtick-theme {
}".NormalizeNewLines();
            var parser = new ScriptParser(source);
            var script = parser.Parse();

            var visitor = new FlatAstVisitor();
            visitor.Traverse(script);

            Assert.Collection(visitor.Nodes,
                decl => ScriptAssert.NodeMatches(source, source, NodeType.Script, decl),
                decl => ScriptAssert.NodeMatches(source, source, NodeType.ActionDeclaration, decl),
                decl => ScriptAssert.NodeMatches(source, "my-action", NodeType.Identifier, decl),
                decl => ScriptAssert.NodeMatches(source, "woodtick-theme", NodeType.Identifier, decl),
                decl => ScriptAssert.NodeMatches(source, "{\n}", NodeType.BlockStatement, decl)
            );
        }

        [Fact]
        public void Assigns_correct_node_locations_multiline_call()
        {
            string source = @"action {
    start-music \
        woodtick-theme
}".NormalizeNewLines();
            var parser = new ScriptParser(source);
            var script = parser.Parse();

            var visitor = new FlatAstVisitor();
            visitor.Traverse(script);

            Assert.Collection(visitor.Nodes,
                decl => ScriptAssert.NodeMatches(source, source, NodeType.Script, decl),
                decl => ScriptAssert.NodeMatches(source, source, NodeType.ActionDeclaration, decl),
                decl => ScriptAssert.NodeMatches(source, "{\n    start-music \\\n        woodtick-theme\n}", NodeType.BlockStatement, decl),
                decl => ScriptAssert.NodeMatches(source, "start-music \\\n        woodtick-theme", NodeType.CallStatement, decl),
                decl => ScriptAssert.NodeMatches(source, "start-music", NodeType.Identifier, decl),
                decl => ScriptAssert.NodeMatches(source, "woodtick-theme", NodeType.Identifier, decl)
            );
        }
    }
}
