using Jither.CommandLine;
using Jither.Imuse.Files;
using Jither.Midi.Files;
using Misty.Conversion;
using Misty.Remapping;

namespace Misty.Verbs;

[Verb("split", Help = "Split MIDI into one file per track.")]
public class SplitOptions : CommonOptions, ICustomParsing
{
    [Positional(0, Help = "Path to input MIDI file.", Name = "input file", Required = true)]
    public string InputFile { get; set; }

    [Positional(1, Help = "Path format for output MIDI files. Default: '{folder}/{name}-{track}.mid'.", Name = "output format", Required = false)]
    public string OutputPathFormat { get; set; }

    [Option('r', "remap", Help = "Remap instruments.")]
    public bool Remap { get; set; }

    [Option('m', "mapping", Help = $"Path to mapping file. Default: {InstrumentMap.DefaultFileName} in program folder.", ArgName = "file")]
    public string MappingFile { get; set; }

    [Examples]
    public static IEnumerable<Example<SplitOptions>> Examples => new[]
    {
        new Example<SplitOptions>("Split using default output path format", new SplitOptions { InputFile = "largo.mid" }),
        new Example<SplitOptions>("Split to current folder using same name", new SplitOptions { InputFile = "D:/midis/largo.mid", OutputPathFormat = "./{name}-{track}.mid" }),
        new Example<SplitOptions>("Split and remap using default output path format", new SplitOptions { InputFile = "largo.mid", Remap = true }),
        new Example<SplitOptions>("Split and remap using custom mapping and default output path format", new SplitOptions { InputFile = "largo.mid", MappingFile = "mycustom.mapping", Remap = true }),
    };

    public void AfterParse()
    {
        OutputPathFormat ??= "{folder}/{name}-{track}.mid";
        if (MappingFile != null)
        {
            Remap = true;
        }
        MappingFile ??= InstrumentMap.DefaultPath;
    }
}

public class SplitCommand : Command<SplitOptions>
{
    public SplitCommand(SplitOptions options) : base(options)
    {
    }

    public override void Execute()
    {
        // Using SoundFile rather than MidiFile to also work with iMUSE chunks
        SoundFile sound;
        try
        {
            logger.Info($"Reading [green]{options.InputFile}[/]...");
            sound = SoundFile.Load(options.InputFile);
        }
        catch (Exception ex) when (ex is MidiFileException or IOException)
        {
            throw new MistyException($"Failed loading MIDI: {ex.Message}");
        }


        if (options.Remap)
        {
            var remapper = new MidiRemapper(options.MappingFile);
            logger.Info("Remapping...");
            remapper.Remap(sound.Midi);
        }

        try
        {
            var inputPath = Path.GetFullPath(options.InputFile);
            var converter = new MidiConverter();
            converter.SaveSplitTracks(sound.Midi, inputPath, options.OutputPathFormat);
        }
        catch (Exception ex) when (ex is MidiFileException or IOException)
        {
            throw new MistyException($"Failed saving split MIDIs: {ex.Message}");
        }

        logger.Info("Done!");
    }
}
