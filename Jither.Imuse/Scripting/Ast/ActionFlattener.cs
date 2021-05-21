using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Ast
{
    /// <summary>
    /// Transforms action bodies into a flat list of statements with jumps (conditional or not) replacing flow control statements.
    /// </summary>
    public class ActionFlattener : IAstVisitor
    {
        private List<Statement> currentActionStatements;
        private readonly Stack<Label> loopEndLabelStack = new();

        public void Execute(Node node)
        {
            node.Accept(this);
        }

        public void VisitActionDeclaration(ActionDeclaration node)
        {
            currentActionStatements = new List<Statement>();
            node.Body.Accept(this);

            // Now we have a flat list of statements for the action, with Label statements and Jump statements referencing the labels.
            // Go through the list, populating the jump statements with the index of their referenced label. Then remove the label, making
            // the jump statement reference the index of the statement immediately after.

            int i = 0;
            while (i < currentActionStatements.Count)
            {
                var stmt = currentActionStatements[i];
                if (stmt is Label label)
                {
                    label.AssignIndex(i);
                    currentActionStatements.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            var body = new BlockStatement(currentActionStatements);
            node.ReplaceBody(body);
        }

        public void VisitBlockStatement(BlockStatement node)
        {
            foreach (var stmt in node.Body)
            {
                stmt.Accept(this);
            }
        }

        public void VisitBreakStatement(BreakStatement node)
        {
            AddJump(loopEndLabelStack.Peek());
        }

        public void VisitBreakHereStatement(BreakHereStatement node)
        {
            Add(node);
        }

        public void VisitCaseStatement(CaseStatement node)
        {
            var endLabel = new Label();
            var caseLabels = new List<Label>();
            bool hasDefault = false;
            foreach (var c in node.Cases)
            {
                var caseLabel = new Label();
                caseLabels.Add(caseLabel);
                // TODO: case default should always be last case (and there should be only one)
                if (c.Test == null)
                {
                    hasDefault = true;
                    AddJump(caseLabel);
                }
                else
                {
                    var test = new BinaryExpression(node.Discriminant, c.Test, BinaryOperator.Equal);
                    AddIf(test, caseLabel);
                }
            }

            if (!hasDefault)
            {
                // If no cases match, and we don't have a default, skip all the case bodies
                AddJump(endLabel);
            }

            int caseIndex = 0;
            foreach (var c in node.Cases)
            {
                var caseLabel = caseLabels[caseIndex];
                Add(caseLabel);
                c.Accept(this);
                AddJump(endLabel);
                caseIndex++;
            }
            AddJump(endLabel); // if no case matches
        }

        public void VisitDoStatement(DoStatement node)
        {
            var startLabel = new Label();
            var endLabel = new Label();
            PushLoopEnd(endLabel); // for break

            Add(startLabel);
            node.Body.Accept(this);
            if (node.Test != null)
            {
                AddIfNot(node.Test, startLabel);
            }
            else
            {
                AddJump(startLabel);
            }
            Add(endLabel); // for break
            PopLoopEnd();
        }

        public void VisitEnqueueStatement(EnqueueStatement node)
        {
            Add(new EnqueueStartStatement(node));
            node.Body.Accept(this);
            Add(new EnqueueEndStatement());
        }

        public void VisitExpressionStatement(ExpressionStatement node)
        {
            Add(node);
        }

        public void VisitForStatement(ForStatement node)
        {
            // Semantics of SCUMM/MUSK for statement, increment:
            // counter = from
            // :start
            //    ...
            // counter++
            // if (counter <= to) jump :start

            // Semantics of SCUMM/MUSK for statement, decrement:
            // counter = from
            // :start
            //    ...
            // counter--
            // if (counter >= to) jump :start

            // counter can be modified inside loop - and often is in original SCUMM.
            var counter = node.Iterator;
            var initialization = new ExpressionStatement(new AssignmentExpression(counter, node.From, AssignmentOperator.Equals));
            var update = new ExpressionStatement(new UpdateExpression(counter, node.Increment ? UpdateOperator.Increment : UpdateOperator.Decrement));
            var test = new BinaryExpression(counter, node.To, node.Increment ? BinaryOperator.LessOrEqual : BinaryOperator.GreaterOrEqual);

            var startLabel = new Label();
            var endLabel = new Label();
            PushLoopEnd(endLabel);

            Add(initialization);
            Add(startLabel);
            node.Body.Accept(this);
            Add(update);
            AddIf(test, startLabel);

            Add(endLabel);
            PopLoopEnd();
        }

        public void VisitIfStatement(IfStatement node)
        {
            var elseLabel = new Label();
            var endLabel = new Label();

            if (node.Alternate != null)
            {
                AddIfNot(node.Test, elseLabel);
            }
            else
            {
                AddIfNot(node.Test, endLabel);
            }
            node.Consequent.Accept(this);
            if (node.Alternate != null)
            {
                AddJump(endLabel);
                Add(elseLabel);
                node.Alternate.Accept(this);
            }
            Add(endLabel);
        }

        public void VisitScript(Script node)
        {
            foreach (var decl in node.Declarations)
            {
                decl.Accept(this);
            }
        }

        public void VisitWhileStatement(WhileStatement node)
        {
            var startLabel = new Label();
            var endLabel = new Label();
            PushLoopEnd(endLabel);
            
            Add(startLabel);
            AddIfNot(node.Test, endLabel);
            node.Body.Accept(this);
            AddJump(startLabel);
            Add(endLabel);
            
            PopLoopEnd();
        }

        private Statement Add(Statement statement)
        {
            currentActionStatements.Add(statement);
            return statement;
        }

        private Statement AddJump(Label destination)
        {
            var statement = new JumpStatement(destination);
            return Add(statement);
        }

        private Statement AddIf(Expression test, Label destination)
        {
            var statement = new ConditionalJumpStatement(destination, test, jumpWhen: true);
            return Add(statement);
        }

        private Statement AddIfNot(Expression test, Label destination)
        {
            var statement = new ConditionalJumpStatement(destination, test, jumpWhen: false);
            return Add(statement);
        }

        private void PushLoopEnd(Label label)
        {
            loopEndLabelStack.Push(label);
        }

        private void PopLoopEnd()
        {
            loopEndLabelStack.Pop();
        }

        public void VisitConditionalJumpStatement(ConditionalJumpStatement node)
        {
        }

        public void VisitJumpStatement(JumpStatement node)
        {
        }

        public void VisitAssignmentExpression(AssignmentExpression node)
        {
        }

        public void VisitBinaryExpression(BinaryExpression node)
        {
        }

        public void VisitDefineDeclaration(DefineDeclaration node)
        {
        }

        public void VisitEventDeclaration(EventDeclaration node)
        {
        }

        public void VisitCallExpression(CallExpression node)
        {
        }

        public void VisitCaseDefinition(CaseDefinition node)
        {
        }

        public void VisitIdentifier(Identifier node)
        {
        }

        public void VisitKeyPressEventDeclarator(KeyPressEventDeclarator node)
        {
        }

        public void VisitLiteral(Literal node)
        {
        }

        public void VisitSoundDeclarator(SoundDeclarator node)
        {
        }

        public void VisitSoundsDeclaration(SoundsDeclaration node)
        {
        }

        public void VisitStartEventDeclarator(StartEventDeclarator node)
        {
        }

        public void VisitTimeEventDeclarator(TimeEventDeclarator node)
        {
        }

        public void VisitUnaryExpression(UnaryExpression node)
        {
        }

        public void VisitUpdateExpression(UpdateExpression expr)
        {
        }

        public void VisitVariableDeclaration(VariableDeclaration node)
        {
        }

        public void VisitEnqueueStartStatement(EnqueueStartStatement node)
        {
        }

        public void VisitEnqueueEndStatement(EnqueueEndStatement node)
        {
        }
    }
}
