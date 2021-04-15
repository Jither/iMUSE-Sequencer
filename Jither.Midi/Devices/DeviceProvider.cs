using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Devices
{
    public abstract class DeviceProvider
    {
        public abstract IEnumerable<DeviceDescription> GetOutputDeviceDescriptions();
        public abstract OutputDevice GetOutputDevice(int deviceId);
        public abstract IEnumerable<DeviceDescription> GetInputDeviceDescriptions();
        public abstract InputDevice GetInputDevice(int deviceId);
    }
}
