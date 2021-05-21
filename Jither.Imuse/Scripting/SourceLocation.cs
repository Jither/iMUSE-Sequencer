namespace Jither.Imuse.Scripting
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
}
