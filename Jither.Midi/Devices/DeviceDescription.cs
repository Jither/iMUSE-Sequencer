using Jither.Utilities;

namespace Jither.Midi.Devices
{
    public class DeviceDescription
    {
        // DeviceID Should probably be a string for cross-platform
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"{Id,2}: {Name}";
        }
    }
}
