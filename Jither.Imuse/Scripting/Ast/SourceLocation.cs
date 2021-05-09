namespace Jither.Imuse.Scripting.Ast
{
    public class SourceLocation
    {
        public int Line { get; }
        public int Column { get; }
        public int Index { get; }

        public SourceLocation(int line, int column, int index)
        {
            Line = line;
            Column = column;
            Index = index;
        }

        public override string ToString()
        {
            return $"{Line}:{Column}";
        }
    }

    public class SourceRange
    {
        public SourceLocation Start { get; }
        public SourceLocation End { get; }

        public SourceRange(SourceLocation start, SourceLocation end)
        {
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            return $"{Start}-{End}";
        }
    }
}
