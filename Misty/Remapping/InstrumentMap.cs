using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Misty.Remapping;

public class InstrumentMap : IReadOnlyDictionary<int, int>
{
    public const string DefaultFileName = "default.mapping";
    public static string DefaultPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DefaultFileName);

    private readonly Dictionary<int, int> mapping = [];

    public InstrumentStandard FromStandard { get; }
    public InstrumentStandard ToStandard { get; }

    public InstrumentMap(string path)
    {
        for (int i = 1; i <= 128; i++)
        {
            mapping.Add(i, i);
        }

        var lines = File.ReadAllLines(path, Encoding.UTF8);
        int lineNumber = 1;
        foreach (var unparsedLine in lines)
        {
            string line = unparsedLine;
            // Comments:
            var commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
                line = line.Remove(commentIndex);
            }
            line = line.Trim();
            if (String.IsNullOrEmpty(line))
            {
                lineNumber++;
                continue;
            }

            bool isMeta = false;

            // Directives:
            if (line.StartsWith('!'))
            {
                isMeta = true;
                line = line[1..];
            }

            var pair = line.Split(':');
            string strFrom = pair[0].Trim();
            string strTo = pair[0].Trim();

            if (pair.Length > 1)
            {
                strTo = pair[1].Trim();
            }

            if (isMeta)
            {
                FromStandard = ParseStandard(strFrom);
                ToStandard = ParseStandard(strTo);
                lineNumber++;
                continue;
            }

            if (!Int32.TryParse(strFrom, out int from))
            {
                throw new MapParserException($"Expected instrument number, but found '{strFrom}'", lineNumber);
            }
            if (!Int32.TryParse(strTo, out int to))
            {
                throw new MapParserException($"Expected instrument number, but found '{strTo}'", lineNumber);
            }
            if (from < 1 || from > 128)
            {
                throw new MapParserException($"Instrument numbers should be between 1 and 128. Was: {from}", lineNumber);
            }
            if (to < 1 || to > 128)
            {
                throw new MapParserException($"Instrument numbers should be between 1 and 128. Was: {to}", lineNumber);
            }

            mapping[from] = to;

            lineNumber++;
        }
    }

    private static InstrumentStandard ParseStandard(string standard)
    {
        return standard.ToLowerInvariant() switch
        {
            "gm" => InstrumentStandard.GeneralMidi,
            "mt32" => InstrumentStandard.RolandMT32,
            "mt-32" => InstrumentStandard.RolandMT32,
            _ => InstrumentStandard.Unknown
        };
    }

    public int this[int key] => mapping[key];

    public IEnumerable<int> Keys => mapping.Keys;

    public IEnumerable<int> Values => mapping.Values;

    public int Count => mapping.Count;

    public bool ContainsKey(int key)
    {
        return mapping.ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<int, int>> GetEnumerator()
    {
        return mapping.GetEnumerator();
    }

    public bool TryGetValue(int key, [MaybeNullWhen(false)] out int value)
    {
        return mapping.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
