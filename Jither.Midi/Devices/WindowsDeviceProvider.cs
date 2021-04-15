using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jither.Midi.Devices
{
    public class WindowsDeviceProvider : DeviceProvider
    {
        [DllImport("winmm.dll")]
        private static extern int midiOutGetNumDevs();
        [DllImport("winmm.dll")]
        private static extern int midiOutGetDevCaps(IntPtr deviceId, ref MidiOutCaps caps, int sizeOfMidiOutCaps);

        public override IEnumerable<DeviceDescription> GetOutputDeviceDescriptions()
        {
            int count = midiOutGetNumDevs();
            MidiOutCaps caps = new MidiOutCaps();
            for (int i = 0; i < count; i++)
            {
                IntPtr deviceId = (IntPtr)i;
                int result = midiOutGetDevCaps(deviceId, ref caps, Marshal.SizeOf<MidiOutCaps>());
                if (result != WindowsMidiDeviceException.MMSYSERR_NOERROR)
                {
                    throw new WindowsMidiDeviceException(result);
                }
                yield return new DeviceDescription
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
