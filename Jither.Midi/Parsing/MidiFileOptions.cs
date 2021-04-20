using System.Collections.Generic;

namespace Jither.Midi.Parsing
{
    public class MidiFileOptions
    {
        private readonly Dictionary<int, ISysexParser> sysexParsers = new();
        public IReadOnlyDictionary<int, ISysexParser> SysexParsers => sysexParsers;

        public MidiFileOptions WithParser(ISysexParser parser)
        {
            sysexParsers.Add(parser.ManufacturerId, parser);
            return this;
        }
    }
}
