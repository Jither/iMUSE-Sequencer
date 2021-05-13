using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting.Ast
{
    public interface IAstVisitor
    {
        void VisitScript(Script node);

        void VisitIdentifier(Identifier node);
        void VisitLiteral(Literal node);
        void VisitBinaryExpression(BinaryExpression node);
        void VisitUnaryExpression(UnaryExpression node);
        void VisitUpdateExpression(UpdateExpression expr);
        void VisitCallExpression(CallExpression node);

        void VisitDefineDeclaration(DefineDeclaration node);
        void VisitSoundsDeclaration(SoundsDeclaration node);
        void VisitActionDeclaration(ActionDeclaration node);
        void VisitSoundDeclarator(SoundDeclarator node);

        void VisitBlockStatement(BlockStatement node);
        void VisitBreakStatement(BreakStatement node);
        void VisitIfStatement(IfStatement node);
        void VisitDoStatement(DoStatement node);
        void VisitExpressionStatement(ExpressionStatement node);
        void VisitWhileStatement(WhileStatement node);
        void VisitForStatement(ForStatement node);
        void VisitCaseStatement(CaseStatement node);
        void VisitCaseDefinition(CaseDefinition node);
        void VisitAssignmentStatement(AssignmentExpression node);
        void VisitEnqueueStatement(EnqueueStatement node);
    }

    /// <summary>
    /// Allows traversal of Abstract Syntax Tree, depth first, using the Visitor pattern.
    /// </summary>
    /// <remarks>
    /// This is useful when each node type has its own specific processing. Because the IAstVisitor interface enforces at compile-time
    /// that each and every node type has a Visit method, this ensures that e.g. new node types in the language aren't missed.
    /// </remarks>
    public class AstTraverser
    {
        private readonly IAstVisitor visitor;

        public AstTraverser(IAstVisitor visitor)
        {
            this.visitor = visitor;
        }

        public void Traverse(Node root)
        {
            Stack<Node> pending = new();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();
                node.Accept(visitor);

                foreach (var child in node.Children.Reverse())
                {
                    pending.Push(child);
                }
            }
        }
    }

    /// <summary>
    /// Allows traversal of Abstract Syntax Tree, depth first, calling a callback function for each node.
    /// </summary>
    /// <remarks>
    /// This is useful when processing doesn't care about node type (e.g. counting nodes, looking at positions etc.) - and doesn't
    /// care for implementing methods for every single type.
    /// </remarks>
    public abstract class SimpleAstVisitor : IAstVisitor
    {
        private readonly AstTraverser traverser;

        protected SimpleAstVisitor()
        {
            traverser = new AstTraverser(this);
        }

        public void Traverse(Node root)
        {
            traverser.Traverse(root);
        }

        protected abstract void Visit(Node node);

        public void VisitActionDeclaration(ActionDeclaration node) => Visit(node);
        public void VisitAssignmentStatement(AssignmentExpression node) => Visit(node);
        public void VisitBinaryExpression(BinaryExpression node) => Visit(node);

        public void VisitBlockStatement(BlockStatement node) => Visit(node);
        public void VisitBreakStatement(BreakStatement node) => Visit(node);
        public void VisitExpressionStatement(ExpressionStatement node) => Visit(node);
        public void VisitCaseDefinition(CaseDefinition node) => Visit(node);
        public void VisitCaseStatement(CaseStatement node) => Visit(node);
        public void VisitDefineDeclaration(DefineDeclaration node) => Visit(node);
        public void VisitDoStatement(DoStatement node) => Visit(node);
        public void VisitEnqueueStatement(EnqueueStatement node) => Visit(node);
        public void VisitForStatement(ForStatement node) => Visit(node);
        public void VisitCallExpression(CallExpression node) => Visit(node);
        public void VisitIdentifier(Identifier node) => Visit(node);
        public void VisitIfStatement(IfStatement node) => Visit(node);
        public void VisitLiteral(Literal node) => Visit(node);
        public void VisitScript(Script node) => Visit(node);
        public void VisitSoundDeclarator(SoundDeclarator node) => Visit(node);
        public void VisitSoundsDeclaration(SoundsDeclaration node) => Visit(node);
        public void VisitUnaryExpression(UnaryExpression node) => Visit(node);
        public void VisitUpdateExpression(UpdateExpression node) => Visit(node);
        public void VisitWhileStatement(WhileStatement node) => Visit(node);
    }

    /// <summary>
    /// Allows traversal of Abstract Syntax Tree, depth first, calling a callback function when entering and leaving each node.
    /// </summary>
    // TODO: Make this a visitor (Like SimpleAstVisitor)
    public class AstEnterLeaveTraverser
    {
        private enum TraversalStage
        {
            Enter,
            Leave
        }

        private class TraversalAction
        {
            public Node Node { get; }
            public TraversalStage Stage { get; }

            public TraversalAction(Node node, TraversalStage stage)
            {
                Node = node;
                Stage = stage;
            }
        }

        private readonly Action<Node> enter;
        private readonly Action<Node> leave;

        public AstEnterLeaveTraverser(Action<Node> enter, Action<Node> leave)
        {
            this.enter = enter;
            this.leave = leave;
        }

        public void Traverse(Node root)
        {
            Stack<TraversalAction> pending = new();
            pending.Push(new TraversalAction(root, TraversalStage.Enter));

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                var node = current.Node;
                if (current.Stage == TraversalStage.Enter)
                {
                    enter(node);
                    pending.Push(new TraversalAction(node, TraversalStage.Leave));
                    foreach (var child in node.Children.Reverse())
                    {
                        pending.Push(new TraversalAction(child, TraversalStage.Enter));
                    }
                }
                else
                {
                    leave(node);
                }
            }
        }
    }

}
