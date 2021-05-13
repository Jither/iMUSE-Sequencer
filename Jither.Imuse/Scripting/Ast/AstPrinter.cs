using Jither.Imuse.Scripting.Ast;
using System.Text;

namespace Jither.Imuse.Scripting.Ast
{
    public class AstPrinter : IAstVisitor
    {
        private readonly StringBuilder builder = new();
        private int indentLevel = 0;

        public string Print(Node node)
        {
            node.Accept(this);

            return builder.ToString();
        }

        private void Output(string str)
        {
            builder.Append(new string(' ', indentLevel * 2));
            builder.AppendLine(str);
        }

        public void VisitAssignmentStatement(AssignmentExpression stmt)
        {
            Output(stmt.Operator.OperatorString());

            indentLevel++;
            stmt.Left.Accept(this);
            stmt.Right.Accept(this);
            indentLevel--;
        }

        public void VisitBinaryExpression(BinaryExpression expr)
        {
            Output(expr.Operator.OperatorString());
            indentLevel++;
            expr.Left.Accept(this);
            expr.Right.Accept(this);
            indentLevel--;
        }

        public void VisitExpressionStatement(ExpressionStatement stmt)
        {
            Output("expr");
            indentLevel++;
            stmt.Expression.Accept(this);
            indentLevel--;
        }

        public void VisitDefineDeclaration(DefineDeclaration decl)
        {
            Output("define");
            indentLevel++;
            decl.Identifier.Accept(this);
            decl.Value.Accept(this);
            indentLevel--;
        }

        public void VisitEnqueueStatement(EnqueueStatement stmt)
        {
            Output("enqueue");
            indentLevel++;

            stmt.SoundId.Accept(this);
            stmt.MarkerId.Accept(this);
            stmt.Body.Accept(this);

            indentLevel--;
        }

        public void VisitCallExpression(CallExpression expr)
        {
            Output("function-call");
            indentLevel++;
            expr.Name.Accept(this);
            foreach (var arg in expr.Arguments)
            {
                arg.Accept(this);
            }
            indentLevel--;
        }

        public void VisitIdentifier(Identifier identifier)
        {
            Output($"identifier {identifier.Name}");
        }

        public void VisitLiteral(Literal literal)
        {
            Output($"literal {literal.Value}");
        }

        public void VisitScript(Script script)
        {
            foreach (var decl in script.Declarations)
            {
                decl.Accept(this);
            }
        }

        public void VisitSoundDeclarator(SoundDeclarator sound)
        {
            Output("sound");
            indentLevel++;
            sound.Id.Accept(this);
            sound.Name.Accept(this);
            indentLevel--;
        }

        public void VisitSoundsDeclaration(SoundsDeclaration decl)
        {
            Output("sounds");
            indentLevel++;
            foreach (var sound in decl.Sounds)
            {
                sound.Accept(this);
            }
            indentLevel--;
        }

        public void VisitActionDeclaration(ActionDeclaration decl)
        {
            Output("action");
            indentLevel++;
            
            decl.Name?.Accept(this);
            decl.During?.Accept(this);
            Output("body:");
            decl.Body.Accept(this);
            
            indentLevel--;
        }

        public void VisitEventDeclaration(EventDeclaration evt)
        {
            Output("event");
            indentLevel++;

            evt.Event.Accept(this);
            evt.ActionDeclaration?.Accept(this);
            evt.ActionName?.Accept(this);

            indentLevel--;
        }

        public void VisitUnaryExpression(UnaryExpression expr)
        {
            Output(expr.Operator.OperatorString());
            indentLevel++;
            expr.Argument.Accept(this);
            indentLevel--;
        }

        public void VisitUpdateExpression(UpdateExpression expr)
        {
            Output(expr.Operator.OperatorString());
            indentLevel++;
            expr.Argument.Accept(this);
            indentLevel--;
        }

        public void VisitIfStatement(IfStatement stmt)
        {
            Output("if");
            indentLevel++;
            stmt.Test.Accept(this);

            Output("consequent:");
            stmt.Consequent.Accept(this);

            if (stmt.Alternate != null)
            {
                Output("alternate:");
                stmt.Alternate.Accept(this);
            }
            indentLevel--;
        }

        public void VisitForStatement(ForStatement stmt)
        {
            Output($"for {(stmt.Increment ? "++" : "--")}");
            indentLevel++;

            stmt.Iterator.Accept(this);
            stmt.From.Accept(this);
            stmt.To.Accept(this);
            Output("body:");
            stmt.Body.Accept(this);

            indentLevel--;
        }

        public void VisitCaseStatement(CaseStatement stmt)
        {
            Output("case");
            indentLevel++;
            stmt.Discriminant.Accept(this);
            Output("cases:");
            indentLevel++;
            foreach (var child in stmt.Cases)
            {
                child.Accept(this);
            }
            indentLevel -= 2;
        }

        public void VisitCaseDefinition(CaseDefinition def)
        {
            Output("of");
            indentLevel++;
            if (def.Test == null)
            {
                Output("default:");
            }
            else
            {
                def.Test.Accept(this);
            }
            def.Consequent.Accept(this);
            indentLevel--;
        }

        public void VisitDoStatement(DoStatement stmt)
        {
            Output("do");

            stmt.Body.Accept(this);

            if (stmt.Test != null)
            {
                Output("until");
                indentLevel++;
                stmt.Test.Accept(this);
                indentLevel--;
            }
        }

        public void VisitBlockStatement(BlockStatement stmt)
        {
            Output("{");
            indentLevel++;
            foreach (var child in stmt.Body)
            {
                child.Accept(this);
            }
            indentLevel--;
            Output("}");
        }

        public void VisitWhileStatement(WhileStatement stmt)
        {
            Output("while");
            indentLevel++;
            stmt.Test.Accept(this);
            stmt.Body.Accept(this);
            indentLevel--;
        }

        public void VisitBreakStatement(BreakStatement stmt)
        {
            Output("break");
        }

        public void VisitKeyPressEventDeclarator(KeyPressEventDeclarator node)
        {
            Output("key");
            indentLevel++;
            node.Key.Accept(this);
            indentLevel--;
        }

        public void VisitTimeEventDeclarator(TimeEventDeclarator node)
        {
            Output("time");
            indentLevel++;
            node.Time?.Accept(this);
            if (node.Measure != null)
            {
                Output("measure:");
                indentLevel++;
                node.Measure.Accept(this);
                indentLevel--;
            }
            if (node.Beat != null)
            {
                Output("beat:");
                indentLevel++;
                node.Beat.Accept(this);
                indentLevel--;
            }
            if (node.Tick != null)
            {
                Output("tick:");
                indentLevel++;
                node.Tick.Accept(this);
                indentLevel--;
            }
            indentLevel--;
        }

        public void VisitStartEventDeclarator(StartEventDeclarator node)
        {
            Output("start");
        }
    }
}
