using Jither.Utilities;

namespace Jither.Midi.Devices
{
    public class DeviceDescription
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Id,2}: {Name}";
        }
    }
}
