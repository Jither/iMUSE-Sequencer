using Jither.Utilities;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Scripting
{
    public class SourceRangeContext
    {
        public IReadOnlyList<string> Lines { get; }
        public int FirstLine { get; }

        public SourceRangeContext(IReadOnlyList<string> lines, int firstLine)
        {
            Lines = lines;
            FirstLine = firstLine;
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

        public SourceRangeContext GetContext(string source, int linesBeforeAndAfter)
        {
            // We'll do this the quick and dirty way - just split the source into lines, rather than searching for
            // line breaks starting from the location - which is somewhat of a minefield of off-by-1 errors.
            var lineIndices = source.GetLineIndices(includeEof: true);

            var lines = new List<string>();

            // Add lines before the line where the range starts:
            int line = Start.Line - 1;
            int linesRemaining = linesBeforeAndAfter;
            while (line >= 0 && linesRemaining > 0)
            {
                int lineStart = lineIndices[line];
                int lineEnd = lineIndices[line + 1];
                string lineContent = source[lineStart..lineEnd].TrimEnd('\r', '\n');
                lines.Insert(0, lineContent);
                if (!String.IsNullOrWhiteSpace(lineContent))
                {
                    linesRemaining--;
                }
                line--;
            }

            int firstLine = line + 1;

            // Add lines that are occupied by the range:
            for (int i = Start.Line; i <= End.Line; i++)
            {
                int lineStart = lineIndices[i];
                int lineEnd = lineIndices[i + 1];
                lines.Add(source[lineStart..lineEnd].TrimEnd('\r', '\n'));
            }

            // Add lines after the line where the range ends:
            line = End.Line + 1;
            linesRemaining = linesBeforeAndAfter;
            while (line < lineIndices.Count - 1 && linesRemaining > 0)
            {
                int lineStart = lineIndices[line];
                int lineEnd = lineIndices[line + 1];
                string lineContent = source[lineStart..lineEnd].TrimEnd('\r', '\n');
                lines.Add(lineContent);
                if (!String.IsNullOrWhiteSpace(lineContent))
                {
                    linesRemaining--;
                }
                line++;
            }

            return new SourceRangeContext(lines, firstLine);
        }

        public override string ToString()
        {
            return $"{Start}-{End}";
        }
    }
}
