using System;
using System.Collections.Generic;

namespace Jither.Midi.Devices
{
    public class MidiDeviceException : Exception
    {
        public MidiDeviceException(string message) : base(message)
        {
        }

        public MidiDeviceException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
