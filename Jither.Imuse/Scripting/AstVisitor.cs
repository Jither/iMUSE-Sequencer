using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse.Scripting
{
    public interface IAstVisitor
    {
        void VisitScript(Script script);

        void VisitIdentifier(Identifier identifier);
        void VisitLiteral(Literal literal);
        void VisitBinaryExpression(BinaryExpression expr);
        void VisitUnaryExpression(UnaryExpression expr);
        void VisitUpdateExpression(UpdateExpression expr);

        void VisitDefineDeclaration(DefineDeclaration decl);
        void VisitSoundsDeclaration(SoundsDeclaration decl);
        void VisitTriggerDeclaration(TriggerDeclaration decl);
        void VisitSoundDeclarator(SoundDeclarator sound);

        void VisitBlockStatement(BlockStatement stmt);
        void VisitBreakStatement(BreakStatement stmt);
        void VisitIfStatement(IfStatement stmt);
        void VisitDoStatement(DoStatement stmt);
        void VisitWhileStatement(WhileStatement stmt);
        void VisitForStatement(ForStatement stmt);
        void VisitCaseStatement(CaseStatement stmt);
        void VisitCaseDefinition(CaseDefinition def);
        void VisitFunctionCallExpression(FunctionCallExpression expr);
        void VisitAssignmentStatement(AssignmentStatement stmt);
        void VisitEnqueueStatement(EnqueueStatement stmt);
        void VisitCallStatement(CallStatement stmt);
    }

    /// <summary>
    /// Allows traversal of Abstract Syntax Tree, depth first, using the Visitor pattern.
    /// </summary>
    /// <remarks>
    /// This is useful when each node type has its own specific processing. Because the IAstVisitor interface enforces at compile-time
    /// that each and every node type has a Visit method, this ensures that e.g. new node types in the language aren't missed.
    /// </remarks>
    public class AstVisitorTraverser
    {
        private readonly IAstVisitor visitor;

        public AstVisitorTraverser(IAstVisitor visitor)
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
    public class AstTraverser
    {
        private readonly Action<Node> callback;

        public AstTraverser(Action<Node> callback)
        {
            this.callback = callback;
        }

        public void Traverse(Node root)
        {
            Stack<Node> pending = new();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();
                callback(node);

                foreach (var child in node.Children.Reverse())
                {
                    pending.Push(child);
                }
            }
        }
    }

    /// <summary>
    /// Allows traversal of Abstract Syntax Tree, depth first, calling a callback function when entering and leaving each node.
    /// </summary>
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
