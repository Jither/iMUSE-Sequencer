using ImuseSequencer.Drivers;
using Jither.CommandLine;
using Jither.Logging;
using Jither.Midi.Devices;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Messages;
using Jither.Midi.Parsing;
using Jither.Midi.Sequencing;
using Jither.Midi.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    public enum TestMethod
    {
        MT32Display,
        Sequencer
    }

    [Verb("test", Help = "Runs test code.")]
    public class TestOptions
    {
        [Positional(0, Help = "Test to run", Name = "test", Required = true)]
        public TestMethod Test { get; set; }

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
            switch (options.Test)
            {
                case TestMethod.MT32Display: TestDisplay(); break;
            }
        }

        private void TestDisplay()
        {
            using (var device = new WindowsOutputDevice(options.DeviceId))
            {
                var roland = new Roland();
                device.SendMessage(new SysexMessage(roland.GenerateSysex(0x80000, Encoding.ASCII.GetBytes("Lucasfilm Games     "))));
            }
        }
    }
}
