using Jither.CommandLine;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Messages;
using System.Text;

namespace ImuseSequencer.Verbs
{
    public enum TestMethod
    {
    }

    [Verb("test", Help = "Runs test code.")]
    public class TestOptions : CommonOptions
    {
        [Positional(0, Help = "Test to run", Name = "test", Required = true)]
        public TestMethod Test { get; set; }

        [Option('o', "output", ArgName = "device id", Default = 0, Help = "Specifies device ID to run tests against.")]
        public int DeviceId { get; set; }
    }

    public class TestCommand : Command
    {
        private readonly TestOptions options;
        public TestCommand(TestOptions options) : base(options)
        {
            this.options = options;
        }

        public override void Execute()
        {
        }
    }
}
