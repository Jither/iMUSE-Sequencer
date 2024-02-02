using Jither.CommandLine;
using Jither.Imuse.Files;
using Jither.Midi.Files;
using Misty.Remapping;
using System.Reflection;

namespace Misty.Verbs;

[Verb("remap", Help = "Remaps instruments")]
public class RemapOptions : CommonOptions, ICustomParsing
{
    [Positional(0, Help = "Path to input MIDI file", Name = "input file", Required = true)]
    public string InputFile { get; set; }

    [Positional(1, Help = "Path to output MIDI file. If not specified, output path will be input path with 'remapped.mid' replacing the extension.")]
    public string OutputFile { get; set; }

    [Option('m', "mapping", Help = $"Path to mapping file. Default: {InstrumentMap.DefaultFileName} in program folder.", ArgName = "file")]
    public string MappingFile { get; set; }

    [Examples]
    public static IEnumerable<Example<RemapOptions>> Examples => new[]
{
        new Example<RemapOptions>("Remap MIDI file using default mapping", new RemapOptions { InputFile = "largo.mid", OutputFile = "largo-gm.mid" }),
        new Example<RemapOptions>("Remap MIDI file using default mapping, outputting to largo.remapped.mid", new RemapOptions { InputFile = "largo.mid" }),
        new Example<RemapOptions>("Remap MIDI file using specific mapping", new RemapOptions { InputFile = "largo.mid", OutputFile = "largo-gm.mid", MappingFile = "mycustom.mapping" }),
    };

    public void AfterParse()
    {
        OutputFile ??= Path.ChangeExtension(InputFile, "remapped.mid");
        MappingFile ??= InstrumentMap.DefaultPath;
    }
}

public class RemapCommand : Command<RemapOptions>
{
    public RemapCommand(RemapOptions options) : base(options)
    {
    }

    public override void Execute()
    {
        logger.Info($"Using mapping file at [green]{options.MappingFile}[/]");


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

        var remapper = new MidiRemapper(options.MappingFile);
        logger.Info("Remapping...");
        remapper.Remap(sound.Midi);

        try
        {
            logger.Info($"Saving as [green]{options.OutputFile}[/]...");
            sound.Midi.Save(options.OutputFile);
        }
        catch (Exception ex) when (ex is MidiFileException or IOException)
        {
            throw new MistyException($"Failed saving remapped MIDI: {ex.Message}");
        }

        logger.Info("Done!");
    }
}
