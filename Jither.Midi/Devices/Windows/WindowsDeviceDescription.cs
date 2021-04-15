using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jither.Utilities;

namespace Jither.Midi.Devices.Windows
{
    public class WindowsDeviceDescription : DeviceDescription
    {
        public int ManufacturerId { get; set; }
        public int ProductId { get; set; }
        public int Version { get; set; }
        public MidiOutputTechnology Technology { get; set; }
        public int Voices { get; set; }
        public int MaxVoices { get; set; }
        public int MaxNotes { get; set; }
        public int ChannelMask { get; set; }
        public int SupportFlags { get; set; }

        public override string ToString()
        {
            return $"{Id,2}: {Name} ({Technology.GetFriendlyName()})";
        }
    }
}
