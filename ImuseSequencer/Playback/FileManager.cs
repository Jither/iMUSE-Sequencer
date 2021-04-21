using ImuseSequencer.Parsing;
using Jither.Logging;
using Jither.Midi.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public class FileManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(FileManager));

        private readonly Dictionary<int, SoundFile> files = new();

        public void Register(int id, SoundFile file)
        {
            files.Add(id, file);
            logger.Info($"Registered sound {id}: {file.Name}");
        }

        public SoundFile Get(int id)
        {
            files.TryGetValue(id, out var file);
            return file;
        }
    }
}
