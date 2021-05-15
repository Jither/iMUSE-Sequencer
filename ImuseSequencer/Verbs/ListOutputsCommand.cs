using Jither.CommandLine;
using Jither.Logging;
using Jither.Midi.Devices;
using Jither.Midi.Devices.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    [Verb("list-outputs", Help = "Lists MIDI output devices")]
    public class ListOutputsOptions : CommonOptions
    {

    }

    public class ListOutputsCommand : Command<ListOutputsOptions>
    {
        public ListOutputsCommand(Settings settings, ListOutputsOptions options) : base(settings, options)
        {
        }

        public override void Execute()
        {
            var provider = new WindowsDeviceProvider();
            var outputs = provider.GetOutputDeviceDescriptions();
            foreach (var output in outputs)
            {
                logger.Info(output.ToString());
            }
        }
    }
}
