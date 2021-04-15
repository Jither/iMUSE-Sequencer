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
    public class ListOutputsOptions
    {

    }

    public class ListOutputsCommand
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ListOutputsCommand));
        private readonly ListOutputsOptions options;

        public ListOutputsCommand(ListOutputsOptions options)
        {
            this.options = options;
        }

        public void Execute()
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
