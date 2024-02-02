using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Files;
using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using Jither.Utilities;

namespace Misty.Remapping;

public class MidiRemapper
{
    private static readonly Logger logger = LogProvider.Get(nameof(MidiRemapper));

    private readonly InstrumentMap map;
    public MidiRemapper(string mappingFile)
    {
        try
        {
            map = new InstrumentMap(mappingFile);
        }
        catch (IOException ex)
        {
            throw new MistyException($"Failed reading mapping file: {ex.Message}");
        }
    }

    public void Remap(MidiFile file)
    {
        foreach (var track in file.Tracks)
        {
            foreach (var evt in track.Events)
            {
                if (evt.Message is ProgramChangeMessage prch)
                {
                    prch.Program = Remap(prch.Program);
                }
                else if (evt.Message is ImuseAllocPart alloc)
                {
                    alloc.Program = Remap((byte)alloc.Program);
                }
                else if (evt.Message is ImuseHookPartProgramChange hook)
                {
                    hook.Program = Remap((byte)hook.Program);
                }
            }
        }
    }

    private byte Remap(byte program)
    {
        return (byte)(map[program + 1] - 1);
    }

    public void Dump()
    {
        logger.Info($"Mapping from [green]{GetStandardName(map.FromStandard)}[/] to [green]{GetStandardName(map.ToStandard)}[/]:");
        Func<int, string> fromNames = GetNameProvider(map.FromStandard);
        Func<int, string> toNames = GetNameProvider(map.ToStandard);
        for (int i = 1; i <= 128; i++)
        {
            logger.Info($"{i,3} [green]{fromNames(i),-25}[/] => {map[i],3} [green]{toNames(map[i]),-25}[/]");
        }
    }

    private static string GetStandardName(InstrumentStandard standard)
    {
        return standard.GetDisplayName();
    }

    private static Func<int, string> GetNameProvider(InstrumentStandard standard)
    {
        return standard switch
        {
            InstrumentStandard.GeneralMidi => num => PatchNameHelper.GeneralMidiNames[num - 1],
            InstrumentStandard.RolandMT32 => num => PatchNameHelper.MT32Names[num - 1],
            _ => num => $"Instrument {num}"
        };
    }
}
