﻿using Jither.Imuse.Scripting.Ast;
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

        private bool enqueuing;

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
                    Keywords.Variable => ParseVariableDeclaration(),
                    Keywords.Sounds => ParseSoundsDeclaration(),
                    Keywords.Action => ParseActionDeclaration(),
                    Keywords.On => ParseEventDeclaration(),
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

            var value = ParseExpression();

            return Finalize(new DefineDeclaration(identifier, value));
        }

        private VariableDeclaration ParseVariableDeclaration()
        {
            StartNode();

            ExpectKeyword(Keywords.Variable);

            var identifier = ParseIdentifier();

            // Initialization of global variable is optional
            Expression value = null;
            if (Matches("="))
            {
                NextToken();

                value = ParseExpression();
            }

            return Finalize(new VariableDeclaration(identifier, value));
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

        private SoundDeclarator ParseSoundDeclarator()
        {
            StartNode();
            var name = ParseStringLiteral("sound name");
            var id = ParseExpression();

            return Finalize(new SoundDeclarator(name, id));
        }

        private ActionDeclaration ParseActionDeclaration()
        {
            StartNode();
            ExpectKeyword(Keywords.Action);

            Expression during = null;
            Identifier id = null;

            // TODO: This should be rewritten - the grammar is 'action' ID? ('during' expression)? '{' - SINGLE id, SINGLE duration, both optional
            while (!Matches("{"))
            {
                if (MatchesSoftKeyword(Keywords.During))
                {
                    NextToken();
                    during = ParseExpression();
                }
                else if (Matches(TokenType.Identifier))
                {
                    id = ParseIdentifier();
                }
                else
                {
                    return ThrowUnexpectedToken<ActionDeclaration>(lookahead);
                }
            }

            var body = ParseBlockStatement();

            return Finalize(new ActionDeclaration(id, during, body));
        }

        private Statement ParseStatement()
        {
            switch (lookahead.Type)
            {
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
                        Keywords.BreakHere => ParseBreakHereStatement(),
                        _ => ThrowUnexpectedToken<Statement>(lookahead, expected: "statement"),
                    };
                case TokenType.Punctuation:
                    if (Matches("{"))
                    {
                        return ParseBlockStatement();
                    }
                    break;
            }
            return ParseExpressionStatement();
        }

        private EventDeclaration ParseEventDeclaration()
        {
            StartNode();

            ExpectKeyword(Keywords.On);

            EventDeclarator decl = null;
            if (lookahead.Type == TokenType.Identifier) // Soft keyword
            {
                switch (lookahead.Value)
                {
                    case Keywords.Key:
                        decl = ParseKeyPressEventDeclarator();
                        break;
                    case Keywords.Time:
                        decl = ParseTimeEventDeclarator();
                        break;
                    case Keywords.Start: // For debugging/testing
                        decl = ParseStartEventDeclarator();
                        break;
                }
            }
            if (decl == null)
            {
                return ThrowUnexpectedToken<EventDeclaration>(lookahead, expected: "event declaration");
            }

            Expect(":");

            if (MatchesKeyword(Keywords.Action))
            {
                var action = ParseActionDeclaration();
                return Finalize(new EventDeclaration(decl, action));
            }
            else if (Matches(TokenType.Identifier))
            {
                var action = ParseIdentifier();
                return Finalize(new EventDeclaration(decl, action));
            }

            return ThrowUnexpectedToken<EventDeclaration>(lookahead, expected: "action declaration or identifier");
        }

        private KeyPressEventDeclarator ParseKeyPressEventDeclarator()
        {
            StartNode();

            ExpectSoftKeyword(Keywords.Key);

            var key = ParseExpression();

            return Finalize(new KeyPressEventDeclarator(key));
        }

        private TimeEventDeclarator ParseTimeEventDeclarator()
        {
            StartNode();

            ExpectSoftKeyword(Keywords.Time);

            var time = ParseExpression();

            return Finalize(new TimeEventDeclarator(time));
        }

        private StartEventDeclarator ParseStartEventDeclarator()
        {
            StartNode();

            ExpectSoftKeyword(Keywords.Start);

            return Finalize(new StartEventDeclarator());
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

        private ExpressionStatement ParseExpressionStatement()
        {
            Expression expression = null;
            StartNode();

            switch (lookahead.Type)
            {
                case TokenType.Identifier:
                    StartNode();
                    var identifier = ParseIdentifier();

                    // Assignment (id = 1)
                    if (Matches("=", "+=", "-=", "*=", "/=", "%=") || MatchesKeyword(Keywords.Is))
                    {
                        expression = ParseAssignmentExpression(identifier);
                    }
                    else
                    {
                        expression = ParseCallExpressionStatement(identifier);
                    }
                    break;
                case TokenType.Punctuation:
                    if (Matches("++", "--"))
                    {
                        expression = ParseUpdateExpression();
                    }
                    break;
            }

            if (expression == null)
            {
                return ThrowUnexpectedToken<ExpressionStatement>(lookahead);
            }

            return Finalize(new ExpressionStatement(expression));
        }

        private EnqueueStatement ParseEnqueueStatement()
        {
            if (enqueuing)
            {
                return ThrowSyntaxError<EnqueueStatement>(lookahead, "enqueue statement cannot be nested");
            }

            enqueuing = true;

            StartNode();
            ExpectKeyword(Keywords.Enqueue);
            var soundId = ParseExpression();

            // Optional "marker" keyword
            if (MatchesSoftKeyword(Keywords.Marker))
            {
                NextToken();
            }
            var markerId = ParseExpression();

            var body = ParseStatement();

            enqueuing = false;
            return Finalize(new EnqueueStatement(soundId, markerId, body));
        }

        private BreakStatement ParseBreakStatement()
        {
            StartNode();
            ExpectKeyword(Keywords.Break);

            return Finalize(new BreakStatement());
        }

        private BreakHereStatement ParseBreakHereStatement()
        {
            int line = CurrentLine;
            StartNode();
            ExpectKeyword(Keywords.BreakHere);

            // Bit of a hack - like call statements, break-here is terminated at newline
            Expression count = null;
            if (line == CurrentLine)
            {
                count = ParseExpression();
            }

            return Finalize(new BreakHereStatement(count));
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
                NextToken();
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
            if (!Matches("=") && !MatchesKeyword("is"))
            {
                return ThrowUnexpectedToken<ForStatement>(lookahead, "for loop expected an = or 'is'", "= or is");
            }
            NextToken();

            var from = ParseExpression();
            ExpectKeyword("to");
            var to = ParseExpression();

            // We need to know at compile-time, whether to increment or decrement.
            // We cannot determine this from to/from expressions, so - like SCUMM - we require ++/-- in the for loop.
            // We also do not set ++ as a "default", because "for x = 2 to 1" would be a rather catastrophic error, yielding 2^32-1 iterations.
            if (!Matches("++", "--"))
            {
                return ThrowUnexpectedToken<ForStatement>(lookahead, "for loop requires an increment/decrement operator", "++ or --");
            }
            var token = NextToken();
            bool increment = token.Value == "++";

            // For loop requires a block statement in SCUMM
            var body = ParseBlockStatement();

            return Finalize(new ForStatement(iterator, from, to, increment, body));
        }

        // Internal for testing
        internal Expression ParseExpression()
        {
            return ParseBinaryExpression();
        }

        private AssignmentExpression ParseAssignmentExpression(Identifier left)
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
                return ThrowUnexpectedToken<AssignmentExpression>(lookahead, expected: "assignment");
            }

            NextToken();

            var right = ParseExpression();

            return Finalize(new AssignmentExpression(left, right, op.Value));
        }

        private Expression ParseCallExpressionStatement(Identifier name)
        {
            // Procedure call statement (with no return value) differs from
            // function call expression in syntax, but not semantics. So, it's an ExpressionStatement(CallExpression)

            // No StartNode() - we started when parsing the identifier
            int line = name.Range.Start.Line;
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

            return Finalize(new CallExpression(name, arguments));
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
            if (Matches("!", "+", "-") || MatchesKeyword(Keywords.Not))
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
                Identifier identifier = ParseIdentifier();
                UpdateOperator op = token.Value switch
                {
                    "++" => UpdateOperator.Increment,
                    "--" => UpdateOperator.Decrement,
                    _ => throw new NotImplementedException($"Update operator {token.Value} not implemented")
                };
                return Finalize(new UpdateExpression(identifier, op));
            }

            return ParsePrimaryExpression();
        }

        private Expression ParsePrimaryExpression()
        {
            return lookahead.Type switch
            {
                TokenType.Identifier => ParseIdentifierOrFunctionCall(),

                TokenType.IntegerLiteral or
                TokenType.TimeLiteral or
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
                return Finalize(new CallExpression(identifier, arguments));
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
                TokenType.TimeLiteral => new Literal(LiteralType.Time, NextToken().TimeValue),
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
        private T ThrowSyntaxError<T>(Token token, string message) where T : Node
        {
            throw new SyntaxException(token, message);
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
                TokenType.TimeLiteral => $"Unexpected time '{token.Value}'",
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

        private bool MatchesKeyword(string value)
        {
            return lookahead.Type == TokenType.Keyword && lookahead.Value == value;
        }

        private bool MatchesSoftKeyword(string value)
        {
            // Soft keywords are just identifiers
            return lookahead.Type == TokenType.Identifier && lookahead.Value == value;
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

        private void ExpectSoftKeyword(string expectedName)
        {
            var token = NextToken();
            if (token.Type != TokenType.Identifier || token.Value != expectedName)
            {
                ThrowUnexpectedToken(token, expected: expectedName);
            }
        }
    }
}
