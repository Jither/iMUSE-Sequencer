using ImuseSequencer.Drivers;
using Jither.CommandLine;
using Jither.Midi.Devices;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    [Verb("test", Help = "Runs test code.")]
    public class TestOptions
    {
        [Option('o', "output", ArgName = "device id", Default = 0, Help = "Specifies device ID to run tests against.")]
        public int DeviceId { get; set; }
    }

    public class TestCommand
    {
        private readonly TestOptions options;
        public TestCommand(TestOptions options)
        {
            this.options = options;
        }

        public void Execute()
        {
            using (var device = new WindowsOutputDevice(options.DeviceId))
            {
                var roland = new Roland();
                device.SendMessage(new SysexMessage(roland.GenerateSysex(0x80000, Encoding.ASCII.GetBytes("Lucasfilm Games     "))));
            }
        }
    }
}
