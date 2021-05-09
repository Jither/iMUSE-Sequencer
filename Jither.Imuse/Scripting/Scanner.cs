using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jither.Imuse.Scripting
{
    public class Keywords
    {
        // Declarations
        public const string Define = "define";
        public const string Sounds = "sounds";
        public const string Action = "action";
        public const string During = "during";

        public const string Break = "break";
        public const string Case = "case";
        public const string Of = "of";
        public const string Default = "default";
        public const string Otherwise = "otherwise";
        public const string Do = "do";
        public const string Until = "until";
        public const string For = "for";
        public const string To = "to";
        public const string If = "if";
        public const string Else = "else";
        public const string While = "while";
        public const string Is = "is";
        public const string Not = "not";
        public const string IsNot = "is-not";
        public const string And = "and";
        public const string Or = "or";

        public const string Enqueue = "enqueue";
        public const string Marker = "marker";

        public static readonly HashSet<string> All = new()
        {
            Define,
            Sounds,
            Action,
            During,

            Break,
            Case,
            Of,
            Default,
            Otherwise,
            Do,
            Until,
            For,
            To,
            If,
            Else,
            While,
            Is,
            Not,
            IsNot,
            And,
            Or,

            Enqueue,
            Marker
        };
    }

    public enum CommentsStyle
    {
        C,
        Lisp
    }

    public class Scanner
    {
        private const CommentsStyle commentsStyle = CommentsStyle.Lisp;

        private readonly string source;
        private readonly int length;
        private int _index;
        private int lineNumber;
        private int lineStartIndex;

        public string Source => Source;
        public int Index => _index;
        public int LineNumber => lineNumber;

        private readonly StringBuilder builder = new();

        private static readonly HashSet<string> twoCharPunctuation = new()
        {
            "==",
            "!=",
            ">=",
            "<=",
            "&&",
            "||",
            "+=",
            "-=",
            "*=",
            "/=",
            "%=",
            "++",
            "--",
        };

        private const string oneCharPunctuation = "<>=!+-*/%\\";

        public Scanner(string source)
        {
            this.source = source;
            _index = 0;
            lineNumber = 0;
            length = source.Length;
            lineStartIndex = 0;
        }

        private char Advance()
        {
            if (_index < length)
            {
                return source[_index++];
            }
            return '\0';
        }

        private char CurrentChar => _index < length ? source[_index] : '\0';
        private char NextChar => _index + 1 < length ? source[_index + 1] : '\0';

        private void ThrowUnexpectedToken(char c)
        {
            throw new ScannerException($"Unexpected token '{c}'", new Location(lineNumber, column: _index - lineStartIndex + 1, _index));
        }

        private void ThrowUnterminatedString()
        {
            throw new ScannerException("Unterminated string", new Location(lineNumber, column: _index - lineStartIndex + 1, _index));
        }

        private static bool IsKeyword(string id)
        {
            return Keywords.All.Contains(id);
        }

        private static bool IsIdentifierCharacter(char c, bool start = false)
        {
            return c == '$' || c == '_' || c == '-' && !start || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' && !start;
        }

        private static bool IsNewLine(char c)
        {
            return c == '\r' || c == '\n';
        }

        private void HandleWindowsNewLine(char first)
        {
            if (first == '\r' && CurrentChar == '\n')
            {
                Advance();
            }
        }

        private void SkipSingleLineComment()
        {
            while (CurrentChar != '\0')
            {
                char c = Advance();
                if (IsNewLine(c))
                {
                    HandleWindowsNewLine(c);
                    lineNumber++;
                    lineStartIndex = _index;
                    return;
                }
            }
        }

        private void SkipMultiLineCStyleComment()
        {
            while (CurrentChar != '\0')
            {
                char c = Advance();
                if (IsNewLine(c))
                {
                    HandleWindowsNewLine(c);
                    lineNumber++;
                    lineStartIndex = _index;
                }
                else if (c == '*')
                {
                    if (CurrentChar == '/')
                    {
                        Advance();
                        return;
                    }
                }
                else
                {
                    Advance();
                }
            }
        }

        private void ScanCommentsAndWhiteSpace()
        {
            while (CurrentChar != '\0')
            {
                var c = CurrentChar;

                if (c == '\r' || c == '\n')
                {
                    Advance();
                    HandleWindowsNewLine(c);
                    lineNumber++;
                    lineStartIndex = _index;
                }
                else if (char.IsWhiteSpace(c))
                {
                    Advance();
                }
                else if (commentsStyle == CommentsStyle.Lisp && c == ';')
                {
                    Advance();
                    SkipSingleLineComment();
                }
                else if (commentsStyle == CommentsStyle.C && c == '/')
                {
                    c = NextChar;
                    if (c == '/')
                    {
                        Advance();
                        Advance();
                        SkipSingleLineComment();
                    }
                    else if (c == '*')
                    {
                        Advance();
                        Advance();
                        SkipMultiLineCStyleComment();
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private string GetIdentifier()
        {
            var start = _index;
            while (CurrentChar != '\0')
            {
                var c = CurrentChar;
                if (IsIdentifierCharacter(c))
                {
                    Advance();
                }
                else
                {
                    break;
                }
            }

            return source[start.._index];
        }

        private Token Token(TokenType type, string value, int startIndex)
        {
            // Currently, all our tokens are on a single line, so we can use current lineNumber for both start and end positions
            var start = new Location(lineNumber, startIndex - lineStartIndex, startIndex);
            var end = new Location(lineNumber, _index - lineStartIndex, _index);
            return new Token(type, value, new Range(start, end));
        }

        private Token ScanIdentifier()
        {
            TokenType type;
            var start = _index;

            var id = GetIdentifier();
            if (IsKeyword(id))
            {
                type = TokenType.Keyword;
            }
            else if (id == "true" || id == "false")
            {
                type = TokenType.BooleanLiteral;
            }
            else
            {
                type = TokenType.Identifier;
            }

            return Token(type, id, start);
        }

        private Token ScanPunctuation()
        {
            string ReadMaxChars(int maxCount)
            {
                int count = _index + maxCount >= length ? length - _index : maxCount;
                return source.Substring(_index, count);
            }

            int start = _index;
            var c = CurrentChar;
            string str = c.ToString();
            switch (c)
            {
                case '{':
                case '}':
                case '(':
                case ')':
                case ';':
                case ',':
                case '.':
                    //case '[':
                    //case ']':
                    //case ':':  // Might need for labels
                    Advance();
                    break;
                default:
                    str = ReadMaxChars(2);
                    if (twoCharPunctuation.Contains(str))
                    {
                        Advance();
                        Advance();
                    }
                    else if (oneCharPunctuation.Contains(c))
                    {
                        str = c.ToString();
                        Advance();
                    }
                    break;
            }

            if (_index == start)
            {
                ThrowUnexpectedToken(c);
            }

            return Token(TokenType.Punctuation, str, start);
        }

        private StringBuilder PrepareStringBuilder()
        {
            builder.Clear();
            return builder;
        }

        private Token ScanNumericLiteral()
        {
            var builder = PrepareStringBuilder();

            int start = _index;
            var c = Advance();

            builder.Append(c);

            c = CurrentChar;
            while (c >= '0' && c <= '9')
            {
                Advance();
                builder.Append(c);
                c = CurrentChar;
            }

            if (c == '.')
            {
                Advance();
                builder.Append(c);
                c = CurrentChar;

                while (c >= '0' && c <= '9')
                {
                    Advance();
                    builder.Append(c);
                    c = CurrentChar;
                }
            }

            // 123abc not allowed
            if (IsIdentifierCharacter(c, start: true))
            {
                ThrowUnexpectedToken(c);
            }
            var number = builder.ToString();

            return Token(TokenType.NumericLiteral, number, start);
        }

        private Token ScanStringLiteral()
        {
            var start = _index;
            var quote = Advance();

            var builder = PrepareStringBuilder();

            bool terminated = false;

            while (CurrentChar != '\0')
            {
                var c = Advance();

                if (c == quote)
                {
                    terminated = true;
                    break;
                }
                else if (c == '\\')
                {
                    c = Advance();
                    if (!IsNewLine(c))
                    {
                        switch (c)
                        {
                            case 'n':
                                builder.Append('\n');
                                break;
                            case 'r':
                                builder.Append('\r');
                                break;
                            case 't':
                                builder.Append('\t');
                                break;
                            default:
                                builder.Append(c);
                                break;
                        }
                    }
                }
                else if (IsNewLine(c))
                {
                    break;
                }
                else
                {
                    builder.Append(c);
                }
            }

            if (!terminated)
            {
                // Backtrack
                _index = start;
                ThrowUnterminatedString();
            }

            return Token(TokenType.StringLiteral, builder.ToString(), start);
        }

        public Token Lex()
        {
            ScanCommentsAndWhiteSpace();

            var c = CurrentChar;

            if (c == '\0')
            {
                return Token(TokenType.EOF, string.Empty, _index);
            }

            if (IsIdentifierCharacter(c, start: true))
            {
                return ScanIdentifier();
            }

            if (c == '"' || c == '\'')
            {
                return ScanStringLiteral();
            }

            if (c >= '0' && c <= '9')
            {
                return ScanNumericLiteral();
            }

            return ScanPunctuation();
        }
    }
}
