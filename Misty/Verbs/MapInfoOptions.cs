using Jither.CommandLine;
using Jither.Midi.Files;
using Misty.Remapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Misty.Verbs;

[Verb("mapinfo", Help = "Outputs info about mapping files.")]
public class MapInfoOptions : CommonOptions, ICustomParsing
{
    [Positional(0, Help = $"Path to mapping file. Default: {InstrumentMap.DefaultFileName} in program folder.", Name = "mapping file", Required = false)]
    public string MappingFile { get; set; }

    public void AfterParse()
    {
        MappingFile ??= InstrumentMap.DefaultPath;
    }
}

public class MapInfoCommand(MapInfoOptions options) : Command<MapInfoOptions>(options)
{
    public override void Execute()
    {
        logger.Info($"Mapping file: [green]{options.MappingFile}[/]");

        var remapper = new MidiRemapper(options.MappingFile);
        remapper.Dump();
    }
}