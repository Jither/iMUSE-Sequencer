using Jither.Imuse.Scripting.Ast;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jither.Imuse.Scripting
{
    public class ScriptParser
    {
        // Next token:
        private Token lookahead;
        private readonly Stack<SourceLocation> startLocations = new();
        // Current token (null when parsing starts) - used for finalizing nodes:
        private Token currentEndToken;

        private int CurrentLine => lookahead.Range.Start.Line;

        private readonly Scanner scanner;

        public ScriptParser(string source)
        {
            scanner = new Scanner(source);
            NextToken();
        }

        public Script Parse()
        {
            StartNode();
            var declarations = new List<Declaration>();
            while (lookahead.Type != TokenType.EOF)
            {
                declarations.Add(ParseDeclaration());
            }

            return Finalize(new Script(declarations));
        }

        private Declaration ParseDeclaration()
        {
            if (Matches(TokenType.Keyword))
            {
                return lookahead.Value switch
                {
                    Keywords.Define => ParseDefineDeclaration(),
                    Keywords.Sounds => ParseSoundsDeclaration(),
                    Keywords.Action => ParseActionDeclaration(),
                    _ => ThrowUnexpectedToken<Declaration>(lookahead),
                };
            }
            return ThrowUnexpectedToken<Declaration>(lookahead);
        }

        private DefineDeclaration ParseDefineDeclaration()
        {
            StartNode();

            ExpectKeyword(Keywords.Define);

            var identifier = ParseIdentifier();

            Expect("=");

            var value = ParseLiteral();

            return Finalize(new DefineDeclaration(identifier, value));
        }

        private SoundsDeclaration ParseSoundsDeclaration()
        {
            StartNode();

            ExpectKeyword(Keywords.Sounds);

            Expect("{");

            var sounds = new List<SoundDeclarator>();
            while (!Matches("}"))
            {
                sounds.Add(ParseSoundDeclarator());
            }

            Expect("}");

            return Finalize(new SoundsDeclaration(sounds));
        }

        private ActionDeclaration ParseActionDeclaration()
        {
            StartNode();
            ExpectKeyword(Keywords.Action);

            Expression during = null;
            Identifier id = null;
            while (!Matches("{"))
            {
                switch (lookahead.Type)
                {
                    case TokenType.Identifier:
                        id = ParseIdentifier();
                        break;
                    case TokenType.Keyword:
                        ExpectKeyword(Keywords.During);
                        during = ParseExpression();
                        break;
                    default:
                        return ThrowUnexpectedToken<ActionDeclaration>(lookahead);
                }
            }

            var body = ParseStatement();

            return Finalize(new ActionDeclaration(id, during, body));
        }

        private Statement ParseStatement()
        {
            switch (lookahead.Type)
            {
                case TokenType.Identifier:
                    StartNode();
                    var left = ParseIdentifier();

                    // Assignment (id = 1)
                    if (Matches("=", "+=", "-=", "*=", "/=", "%=") || MatchesKeyword("is"))
                    {
                        return ParseAssignment(left);
                    }

                    // Call (id arg1 arg2...)
                    return ParseCallStatement(left);

                case TokenType.Keyword:
                    return lookahead.Value switch
                    {
                        // Note: SCUMM doesn't actually have a break statement (not to be confused with break-here)
                        Keywords.Break => ParseBreakStatement(),
                        Keywords.Case => ParseCaseStatement(),
                        Keywords.Do => ParseDoStatement(),
                        Keywords.While => ParseWhileStatement(),
                        Keywords.Enqueue => ParseEnqueueStatement(),
                        Keywords.For => ParseForStatement(),
                        Keywords.If => ParseIfStatement(),
                        _ => ThrowUnexpectedToken<Statement>(lookahead, expected: "statement"),
                    };
                case TokenType.Punctuation:
                    return lookahead.Value switch
                    {
                        "{" => ParseBlockStatement(),
                        _ => ThrowUnexpectedToken<Statement>(lookahead, expected: "statement")
                    };
                default:
                    return ThrowUnexpectedToken<Statement>(lookahead, expected: "statement");
            }
        }

        private BlockStatement ParseBlockStatement()
        {
            StartNode();

            Expect("{");

            var statements = new List<Statement>();
            while (!Matches("}"))
            {
                var statement = ParseStatement();
                statements.Add(statement);
            }

            Expect("}");

            return Finalize(new BlockStatement(statements));
        }

        private BreakStatement ParseBreakStatement()
        {
            StartNode();
            ExpectKeyword(Keywords.Break);

            return Finalize(new BreakStatement());
        }

        private DoStatement ParseDoStatement()
        {
            StartNode();
            ExpectKeyword(Keywords.Do);

            // Requires a block statement in SCUMM
            var body = ParseBlockStatement();

            Expression test = null;
            if (MatchesKeyword(Keywords.Until))
            {
                // SCUMM requires parentheses here - should we?
                test = ParseExpression();
            }

            return Finalize(new DoStatement(body, test));
        }

        private WhileStatement ParseWhileStatement()
        {
            StartNode();
            ExpectKeyword(Keywords.While);

            // SCUMM requires parentheses here - should we?
            var test = ParseExpression();

            // Requires a block statement in SCUMM
            var body = ParseBlockStatement();

            return Finalize(new WhileStatement(test, body));
        }

        private CaseStatement ParseCaseStatement()
        {
            StartNode();
            ExpectKeyword(Keywords.Case);

            var discriminant = ParseExpression();

            Expect("{");

            var cases = new List<CaseDefinition>();
            Statement consequent;

            while (!Matches("}"))
            {
                if (!Matches(TokenType.Keyword))
                {
                    return ThrowUnexpectedToken<CaseStatement>(lookahead, expected: $"{Keywords.Of}, {Keywords.Default} or {Keywords.Otherwise}");
                }
                var keyword = NextToken();
                switch (keyword.Value)
                {
                    case Keywords.Of:
                        StartNode();
                        var test = ParseLiteral();
                        // I think cases make more sense requiring a block statement (and they do in SCUMM)
                        // - since there's no break, a single-statement case without braces may confuse
                        consequent = ParseBlockStatement();
                        cases.Add(Finalize(new CaseDefinition(test, consequent)));
                        break;

                    case Keywords.Default:
                    case Keywords.Otherwise:
                        StartNode();
                        consequent = ParseBlockStatement();
                        cases.Add(Finalize(new CaseDefinition(null, consequent)));
                        break;
                }
            }

            Expect("}");

            return Finalize(new CaseStatement(discriminant, cases));
        }

        private AssignmentStatement ParseAssignment(Identifier left)
        {
            AssignmentOperator? op = lookahead.Type switch
            {
                TokenType.Punctuation => lookahead.Value switch
                {
                    "=" => AssignmentOperator.Equals,
                    "+=" => AssignmentOperator.Add,
                    "-=" => AssignmentOperator.Subtract,
                    "*=" => AssignmentOperator.Multiply,
                    "/=" => AssignmentOperator.Divide,
                    "%=" => AssignmentOperator.Modulo,
                    _ => null
                },
                TokenType.Keyword => lookahead.Value == Keywords.Is ? AssignmentOperator.Equals : null,
                _ => null
            };
            if (op == null)
            {
                return ThrowUnexpectedToken<AssignmentStatement>(lookahead, expected: "assignment");
            }

            NextToken();

            var right = ParseExpression();

            return Finalize(new AssignmentStatement(left, right, op.Value));
        }

        private IfStatement ParseIfStatement()
        {
            StartNode();
            ExpectKeyword("if");

            // SCUMM requires parentheses here - should we?
            var condition = ParseExpression();

            var consequent = ParseStatement();
            Statement alternate = null;
            if (MatchesKeyword(Keywords.Else))
            {
                NextToken();
                alternate = ParseStatement();
            }
            return Finalize(new IfStatement(condition, consequent, alternate));
        }

        private ForStatement ParseForStatement()
        {
            StartNode();
            ExpectKeyword("for");

            var iterator = ParseIdentifier();
            Expect("=");
            var from = ParseExpression();
            ExpectKeyword("to");
            var to = ParseExpression();

            bool increment = true;
            if (Matches("++", "--"))
            {
                var token = NextToken();
                increment = token.Value == "++";
            }

            // For loop requires a block statement in SCUMM
            var body = ParseBlockStatement();

            return Finalize(new ForStatement(iterator, from, to, increment, body));
        }

        private CallStatement ParseCallStatement(Identifier name)
        {
            int line = CurrentLine;
            var arguments = new List<Expression>();
            while (CurrentLine == line && !Matches("}"))
            {
                if (Matches("\\"))
                {
                    // Arguments continue on next line
                    NextToken();
                    line++;
                }
                else
                {
                    arguments.Add(ParseExpression());
                }
            }

            return Finalize(new CallStatement(name, arguments));
        }

        private EnqueueStatement ParseEnqueueStatement()
        {
            StartNode();
            ExpectKeyword(Keywords.Enqueue);
            var soundId = ParseExpression();

            // Optional "marker" keyword
            if (MatchesKeyword(Keywords.Marker))
            {
                NextToken();
            }
            var markerId = ParseExpression();

            var body = ParseStatement();

            return Finalize(new EnqueueStatement(soundId, markerId, body));
        }

        private SoundDeclarator ParseSoundDeclarator()
        {
            StartNode();
            var name = ParseStringLiteral("sound name");
            var id = ParseExpression();

            // TODO: May not belong in the parser (type checking)
            if (id is not Identifier && id is not Literal { ValueType: LiteralType.Integer })
            {
                return ThrowError<SoundDeclarator>("sound ID should be an integer constant or literal", id.Range.Start);
            }

            return Finalize(new SoundDeclarator(name, id));
        }

        // Internal for testing
        internal Expression ParseExpression()
        {
            return ParseBinaryExpression();
        }

        private Expression ParsePrimaryExpression()
        {
            return lookahead.Type switch
            {
                TokenType.Identifier => ParseIdentifierOrFunctionCall(),
                
                TokenType.IntegerLiteral or 
                TokenType.NumericLiteral or 
                TokenType.StringLiteral or 
                TokenType.BooleanLiteral => ParseLiteral(),
                
                TokenType.Punctuation => lookahead.Value switch
                {
                    "(" => ParseGroupExpression(),
                    _ => ThrowUnexpectedToken<Expression>(lookahead, expected: "expression"),
                },
                
                _ => ThrowUnexpectedToken<Expression>(lookahead, expected: "expression"),
            };
            ;
        }

        private Expression ParseGroupExpression()
        {
            Expect("(");

            var result = ParseExpression();

            Expect(")");

            return result;
        }

        private class BinaryOperatorPseudoExpression : Expression
        {
            public BinaryOperator Operator { get; }
            public int Precedence { get; }

            public override NodeType Type => throw new NotImplementedException($"{nameof(BinaryOperatorPseudoExpression)} is not actually a node.");
            public override IEnumerable<Node> Children => throw new NotImplementedException($"{nameof(BinaryOperatorPseudoExpression)} is not actually a node.");
            public override void Accept(IAstVisitor visitor) => throw new NotImplementedException($"{nameof(BinaryOperatorPseudoExpression)} is not actually a node.");

            public BinaryOperatorPseudoExpression(Token token)
            {
                Precedence = 0;
                Operator = (BinaryOperator)(-1);

                if (token.Type == TokenType.Punctuation)
                {
                    switch (token.Value)
                    {
                        case "||": Precedence = 5; Operator = BinaryOperator.Or; break;
                        case "&&": Precedence = 6; Operator = BinaryOperator.And; break;

                        case "==": Precedence = 10; Operator = BinaryOperator.Equal; break;
                        case "!=": Precedence = 10; Operator = BinaryOperator.NotEqual; break;

                        case "<": Precedence = 11; Operator = BinaryOperator.Less; break;
                        case ">": Precedence = 11; Operator = BinaryOperator.Greater; break;
                        case "<=": Precedence = 11; Operator = BinaryOperator.LessOrEqual; break;
                        case ">=": Precedence = 11; Operator = BinaryOperator.GreaterOrEqual; break;

                        case "+": Precedence = 15; Operator = BinaryOperator.Add; break;
                        case "-": Precedence = 15; Operator = BinaryOperator.Subtract; break;

                        case "*": Precedence = 20; Operator = BinaryOperator.Multiply; break;
                        case "/": Precedence = 20; Operator = BinaryOperator.Divide; break;
                        case "%": Precedence = 20; Operator = BinaryOperator.Modulo; break;
                    }
                }
                else if (token.Type == TokenType.Keyword)
                {
                    switch (token.Value)
                    {
                        case Keywords.Or: Precedence = 5; Operator = BinaryOperator.Or; break;
                        case Keywords.And: Precedence = 6; Operator = BinaryOperator.And; break;
                        case Keywords.Is: Precedence = 10; Operator = BinaryOperator.Equal; break;
                        case Keywords.IsNot: Precedence = 10; Operator = BinaryOperator.NotEqual; break;
                    }
                }
            }
        }

        private Expression ParseBinaryExpression()
        {
            Expression expr = ParseUnaryExpression();

            var token = lookahead;
            var op = new BinaryOperatorPseudoExpression(token);
            if (op.Precedence > 0)
            {
                NextToken();

                Expression left = expr;
                Expression right = ParseUnaryExpression();
                var stack = new Stack<Expression>();
                var precedenceStack = new Stack<int>();
                stack.Push(left);
                stack.Push(op);
                stack.Push(right);
                precedenceStack.Push(op.Precedence);

                while (true)
                {
                    op = new BinaryOperatorPseudoExpression(lookahead);
                    if (op.Precedence <= 0)
                    {
                        break;
                    }
                    // Advance from operator
                    NextToken();

                    while (stack.Count > 2 && op.Precedence <= precedenceStack.Peek())
                    {
                        right = stack.Pop();
                        var nextOp = stack.Pop() as BinaryOperatorPseudoExpression;
                        precedenceStack.Pop();
                        left = stack.Pop();

                        StartNode(left.Range.Start);
                        stack.Push(Finalize(new BinaryExpression(left, right, nextOp.Operator), right.Range.End));
                    }

                    stack.Push(op);
                    precedenceStack.Push(op.Precedence);
                    stack.Push(ParseUnaryExpression());
                }

                expr = stack.Pop();
                while (stack.Count > 0)
                {
                    var nextOp = stack.Pop() as BinaryOperatorPseudoExpression;
                    left = stack.Pop();
                    StartNode(left.Range.Start);
                    expr = Finalize(new BinaryExpression(left, expr, nextOp.Operator), right.Range.End);
                }
            }

            return expr;
        }

        private Expression ParseUnaryExpression()
        {
            if (Matches("!", "+", "-") || MatchesKeyword("not"))
            {
                StartNode();
                var token = NextToken();
                Expression expr = ParseUnaryExpression();
                UnaryOperator op = token.Value switch
                {
                    "!" or "not" => UnaryOperator.Not,
                    "-" => UnaryOperator.Minus,
                    "+" => UnaryOperator.Plus,
                    _ => throw new NotImplementedException($"Unary operator {token.Value} not implemented")
                };
                return Finalize(new UnaryExpression(expr, op));
            }

            return ParseUpdateExpression();
        }

        private Expression ParseUpdateExpression()
        {
            // Prefix ONLY (due to x-- being an identifier... That's SCUMM for you)
            if (Matches("++", "--"))
            {
                StartNode();
                var token = NextToken();
                Expression expr = ParseUnaryExpression();
                UpdateOperator op = token.Value switch
                {
                    "++" => UpdateOperator.Increment,
                    "--" => UpdateOperator.Decrement,
                    _ => throw new NotImplementedException($"Update operator {token.Value} not implemented")
                };
                return Finalize(new UpdateExpression(expr, op, prefix: true));
            }

            return ParsePrimaryExpression();

            // Postfix
            /*
            var startToken = lookahead;
            expr = ParsePrimaryExpression();
            if (Matches("++", "--"))
            {
                StartNode(startToken.Range.Start);
                var token = NextToken();
                UpdateOperator op = token.Value switch
                {
                    "++" => UpdateOperator.Increment,
                    "--" => UpdateOperator.Decrement,
                    _ => throw new NotImplementedException($"Update operator {token.Value} not implemented")
                };
                return Finalize(new UpdateExpression(expr, op, prefix: false));
            }
            */
        }

        private Identifier ParseIdentifier()
        {
            StartNode();

            if (!Matches(TokenType.Identifier))
            {
                return ThrowUnexpectedToken<Identifier>(lookahead, expected: "identifier");
            }

            return Finalize(new Identifier(NextToken().Value));
        }

        private Expression ParseIdentifierOrFunctionCall()
        {
            StartNode();
            Identifier identifier = ParseIdentifier();
            if (Matches("("))
            {
                NextToken();
                var arguments = new List<Expression>();
                while (true)
                {
                    arguments.Add(ParseExpression());
                    if (Matches(")"))
                    {
                        break;
                    }
                    Expect(",");
                }
                Expect(")");
                return Finalize(new FunctionCallExpression(identifier, arguments));
            }

            // Not a function call - pop the stored start position
            startLocations.Pop();

            return identifier;
        }

        private Literal ParseLiteral()
        {
            StartNode();
            var result = lookahead.Type switch
            {
                TokenType.BooleanLiteral => new Literal(LiteralType.Boolean, NextToken().Value == "true"),
                TokenType.IntegerLiteral => new Literal(LiteralType.Integer, NextToken().IntegerValue),
                TokenType.NumericLiteral => new Literal(LiteralType.Number, NextToken().NumericValue),
                TokenType.StringLiteral => new Literal(LiteralType.String, NextToken().Value),
                _ => ThrowUnexpectedToken<Literal>(lookahead, expected: "literal"),
            };
            return Finalize(result);
        }

        private Literal ParseStringLiteral(string purpose)
        {
            if (!Matches(TokenType.StringLiteral))
            {
                return ThrowUnexpectedToken<Literal>(lookahead, expected: purpose);
            }
            return ParseLiteral();
        }

        private void StartNode(SourceLocation startLocation = null)
        {
            startLocations.Push(startLocation ?? lookahead.Range.Start);
        }

        private T Finalize<T>(T node, SourceLocation endLocation = null) where T : Node
        {
            node.Finalize(startLocations.Pop(), endLocation ?? currentEndToken.Range.End);
            return node;
        }

        [DoesNotReturn]
        private T ThrowUnexpectedToken<T>(Token token, string message = null, string expected = null) where T : Node
        {
            ThrowUnexpectedToken(token, message, expected);
            return null;
        }

        [DoesNotReturn]
        private void ThrowUnexpectedToken(Token token, string message = null, string expected = null)
        {
            message ??= token.Type switch
            {
                TokenType.BooleanLiteral => "Unexpected boolean",
                TokenType.EOF => "Unexpected end of script",
                TokenType.Identifier => $"Unexpected identifier '{token.Value}'",
                TokenType.IntegerLiteral => $"Unexpected integer '{token.Value}'",
                TokenType.Keyword => $"Unexpected keyword '{token.Value}'",
                TokenType.NumericLiteral => $"Unexpected number '{token.Value}'",
                TokenType.Punctuation => $"Unexpected {token.Value}",
                TokenType.StringLiteral => $"Unexpected string '{token.Value}'",
                _ => "Unexpected token '{token.Value}'"
            };

            if (expected != null)
            {
                message += $". Expected {expected}";
            }
            throw new UnexpectedTokenException(token, message);
        }

        [DoesNotReturn]
        private void ThrowError(string message, SourceLocation start = null, SourceLocation end = null)
        {
            if (start == null)
            {
                start = lookahead.Range.Start;
                end = lookahead.Range.End;
            }
            else if (end == null)
            {
                end = start;
            }
            throw new InvalidTokenException(message, new SourceRange(start, end));
        }

        [DoesNotReturn]
        private T ThrowError<T>(string message, SourceLocation location = null) where T : Node
        {
            ThrowError(message, location);
            return null;
        }

        [MemberNotNull(nameof(lookahead))]
        private Token NextToken()
        {
            currentEndToken = lookahead;
            lookahead = scanner.Lex();
            return currentEndToken;
        }

        private bool Matches(params string[] values)
        {
            return lookahead.Type == TokenType.Punctuation && Array.IndexOf(values, lookahead.Value) >= 0;
        }

        private bool MatchesKeyword(params string[] values)
        {
            return lookahead.Type == TokenType.Keyword && Array.IndexOf(values, lookahead.Value) >= 0;
        }

        private bool Matches(TokenType type)
        {
            return lookahead.Type == type;
        }

        private void Expect(string expectedPunctuation)
        {
            var token = NextToken();
            if (token.Type != TokenType.Punctuation || expectedPunctuation != token.Value)
            {
                ThrowUnexpectedToken(token, expected: expectedPunctuation);
            }
        }

        private void ExpectKeyword(string expectedName)
        {
            var token = NextToken();
            if (token.Type != TokenType.Keyword || token.Value != expectedName)
            {
                ThrowUnexpectedToken(token, expected: expectedName);
            }
        }
    }
}
