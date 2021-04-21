using Jither.Logging;
using System.Collections.Generic;

namespace Jither.Imuse.Files
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
