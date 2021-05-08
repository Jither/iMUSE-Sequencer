using Jither.Imuse.Scripting.Ast;
using System.Text;

namespace Jither.Imuse.Scripting
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

        public void VisitAssignmentStatement(AssignmentStatement stmt)
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

        public void VisitCallStatement(CallStatement stmt)
        {
            Output("call");
            indentLevel++;
            stmt.Name.Accept(this);
            foreach (var arg in stmt.Arguments)
            {
                arg.Accept(this);
            }
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
            foreach (var child in stmt.Body)
            {
                child.Accept(this);
            }
            indentLevel--;
        }

        public void VisitFunctionCallExpression(FunctionCallExpression expr)
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

        public void VisitTriggerDeclaration(TriggerDeclaration decl)
        {
            Output("trigger");
            indentLevel++;
            decl.Id?.Accept(this);
            decl.During?.Accept(this);
            Output("body:");
            indentLevel++;
            foreach (var stmt in decl.Body)
            {
                stmt.Accept(this);
            }
            indentLevel -= 2;
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
            stmt.Condition.Accept(this);

            Output("consequent:");
            indentLevel++;
            foreach (var child in stmt.Consequent)
            {
                child.Accept(this);
            }
            indentLevel--;
            if (stmt.Alternate != null)
            {
                Output("alternate:");
                indentLevel++;
                foreach (var child in stmt.Alternate)
                {
                    child.Accept(this);
                }
                indentLevel--;
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
            indentLevel++;
            foreach (var child in stmt.Body)
            {
                child.Accept(this);
            }
            indentLevel -= 2;
        }
    }
}
