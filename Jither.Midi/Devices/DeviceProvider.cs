using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Devices
{
    public abstract class DeviceProvider
    {
        public abstract IEnumerable<DeviceDescription> GetOutputDeviceDescriptions();
        
        public OutputDevice GetOutputDevice(string selector)
        {
            var descriptions = GetOutputDeviceDescriptions();
            foreach (var desc in descriptions)
            {
                if (desc.Id == selector)
                {
                    return GetOutputDeviceById(desc.Id, desc.Name);
                }
                if (desc.Name.Contains(selector))
                {
                    return GetOutputDeviceById(desc.Id, desc.Name);
                }
            }
            throw new MidiDeviceException($"Couldn't find MIDI output device matching selector {selector}");
        }

        public OutputStream GetOutputStream(string selector)
        {
            var descriptions = GetOutputDeviceDescriptions();
            foreach (var desc in descriptions)
            {
                if (desc.Id == selector)
                {
                    return GetOutputStreamById(desc.Id, desc.Name);
                }
                if (desc.Name.Contains(selector))
                {
                    return GetOutputStreamById(desc.Id, desc.Name);
                }
            }
            throw new MidiDeviceException($"Couldn't find MIDI output stream matching selector {selector}");
        }

        protected abstract OutputDevice GetOutputDeviceById(string id, string name);
        protected abstract OutputStream GetOutputStreamById(string id, string name);
    }
}
