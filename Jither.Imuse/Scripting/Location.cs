namespace Jither.Imuse.Scripting
{
    public class Location
    {
        public int Line { get; }
        public int Column { get; }
        public int Index { get; }

        public Location(int line, int column, int index)
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

    public class Range
    {
        public Location Start { get; }
        public Location End { get; }

        public Range(Location start, Location end)
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
