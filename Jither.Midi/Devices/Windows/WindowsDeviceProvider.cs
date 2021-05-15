using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jither.Midi.Devices.Windows
{
    public class WindowsDeviceProvider : DeviceProvider
    {
        public override IEnumerable<DeviceDescription> GetOutputDeviceDescriptions()
        {
            int count = WinApi.midiOutGetNumDevs();
            MidiOutCaps caps = new();
            for (int i = 0; i < count; i++)
            {
                IntPtr deviceId = (IntPtr)i;
                int result = WinApi.midiOutGetDevCaps(deviceId, ref caps, Marshal.SizeOf<MidiOutCaps>());
                if (result != WindowsMidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new WindowsMidiDeviceException(result);
                }
                yield return new DeviceDescription
                {
                    Id = i.ToString(),
                    Name = caps.name
                };
            }
        }

        protected override OutputDevice GetOutputDeviceById(string deviceId, string name)
        {
            int id = Int32.Parse(deviceId);
            return new WindowsOutputDevice(id, name);
        }

        protected override OutputStream GetOutputStreamById(string deviceId, string name)
        {
            int id = Int32.Parse(deviceId);
            return new WindowsOutputStream(id, name);
        }
    }
}
