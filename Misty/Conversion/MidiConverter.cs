using Jither.Logging;
using Jither.Midi.Files;
using Jither.Midi.Messages;
using Jither.Utilities;

namespace Misty.Conversion;

public class ConvertOptions
{
    public string OutputPathFormat { get; set; }
}

public class MidiConverter
{
    private static readonly Logger logger = LogProvider.Get(nameof(MidiConverter));

    public MidiConverter()
    {

    }

    public void SaveSplitTracks(MidiFile inputFile, string inputPath, string outputPathFormat)
    {
        foreach (var inputTrack in inputFile.Tracks)
        {
            var output = new MidiFile(1, inputFile.DivisionType, inputFile.DivisionType == DivisionType.Ppqn ? inputFile.TicksPerQuarterNote : inputFile.TicksPerFrame);

            var outputTrack = new List<MidiEvent>();
            outputTrack.AddRange(inputTrack.Events);
            output.AddTrack(outputTrack);

            string outputPath = outputPathFormat
                .Replace("{folder}", Path.GetDirectoryName(inputPath))
                .Replace("{name}", Path.GetFileNameWithoutExtension(inputPath))
                .Replace("{track}", (inputTrack.Index + 1).ToString())
                .Replace("{trackindex}", (inputTrack.Index).ToString());

            outputPath = PathHelpers.NormalizeForPlatform(outputPath);

            logger.Info($"Saving track [green]{inputTrack.Index + 1}[/] to [green]{outputPath}[/green]...");

            output.Save(outputPath);
        }
    }
}
