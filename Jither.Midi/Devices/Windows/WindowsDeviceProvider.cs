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
                yield return new WindowsDeviceDescription
                {
                    Id = i,
                    ManufacturerId = caps.mid,
                    ProductId = caps.pid,
                    Name = caps.name,
                    Version = caps.driverVersion,
                    Technology = (MidiOutputTechnology)caps.technology,
                    MaxVoices = caps.voices,
                    MaxNotes = caps.notes,
                    ChannelMask = caps.channelMask,
                    SupportFlags = caps.support
                };
            }
        }

        public override OutputDevice GetOutputDevice(int deviceId)
        {
            return new WindowsOutputDevice(deviceId);
        }

        public override IEnumerable<DeviceDescription> GetInputDeviceDescriptions()
        {
            throw new NotImplementedException();
        }

        public override InputDevice GetInputDevice(int deviceId)
        {
            throw new NotImplementedException();
        }
    }
}
