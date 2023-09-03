using Jither.Imuse.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting
{
    public class FileProvider
    {
        private readonly string folderPath;
        private readonly SoundTarget target;

        public FileProvider(string folderPath, SoundTarget target)
        {
            this.folderPath = NormalizePath(folderPath);

            this.target = target;
        }

        public SoundFile Load(string name)
        {
            string path = GetPath(name);
            try
            {
                return SoundFile.Load(path);
            }
            catch (IOException ex)
            {
                throw new ImuseException($"Could not load sound file '{name}': {ex.Message}");
            }
        }

        private string GetPath(string name)
        {
            string path = Path.Combine(folderPath, name);

            string fullPath;
            // FileProvider assumes files with chunk name extensions
            switch (target)
            {
                case SoundTarget.Adlib:
                    fullPath = Path.ChangeExtension(path, ".adl");
                    break;
                case SoundTarget.Roland:
                    fullPath = Path.ChangeExtension(path, ".rol");
                    break;
                case SoundTarget.GeneralMidi:
                    fullPath = Path.ChangeExtension(path, ".gmd");
                    // General MIDI target may have multiple names
                    // (actually, "MIDI" in iMUSE v3 is a generic MIDI chunk that the driver should translate to the target, but for now...)
                    // TODO: Find a way to handle MIDI generic chunks
                    if (!File.Exists(fullPath))
                    {
                        fullPath = Path.ChangeExtension(path, ".midi");
                    }
                    if (!File.Exists(fullPath))
                    {
                        fullPath = Path.ChangeExtension(path, ".mid");
                    }
                    break;
                case SoundTarget.Speaker:
                    fullPath = Path.ChangeExtension(path, ".spk");
                    break;
                default:
                    throw new NotImplementedException($"FileProvider doesn't support target {target}");
            }

            return NormalizePath(fullPath);
        }

        private string NormalizePath(string path)
        {
            // We're allowing / in paths on Windows (and \ on linux for that matter) - avoids having to escape the paths in e.g. json
            // Actually, Windows allows it too, but to get nice OS-consistent logging output etc., we'll fix the path here.
            // (on linux, AltDirSepChar and DirSepChar are both /, so this will be a "no-op"
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}
