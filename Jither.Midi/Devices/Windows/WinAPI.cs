using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Devices.Windows
{
    public class WinApiConstants
    {
        public const int CALLBACK_NULL = 0x0;
        public const int CALLBACK_WINDOW = 0x10000;
        public const int CALLBACK_TASK = 0x20000;
        public const int CALLBACK_THREAD = CALLBACK_TASK;
        public const int CALLBACK_FUNCTION = 0x30000;
        public const int CALLBACK_EVENT = 0x50000;

        public const int MOM_OPEN = 0x3C7;
        public const int MOM_CLOSE = 0x3C8;
        public const int MOM_DONE = 0x3C9;
    }

    public enum MidiOutputTechnology
    {
        [EnumMember(Value = "MIDI port")]
        MOD_MIDIPORT = 1,
        [EnumMember(Value = "Synth")]
        MOD_SYNTH    = 2,
        [EnumMember(Value = "Square Wave Synth")]
        MOD_SQSYNTH  = 3,
        [EnumMember(Value = "FM Synth")]
        MOD_FMSYNTH  = 4,
        [EnumMember(Value = "MIDI Mapper")]
        MOD_MAPPER   = 5,
        [EnumMember(Value = "WaveTable Synth")]
        MOD_WAVETABLE= 6,
        [EnumMember(Value = "Software Synth")]
        MOD_SWSYNTH  = 7
    }

    /// <summary>
    /// <see href="https://docs.microsoft.com/en-us/windows/win32/api/mmeapi/ns-mmeapi-midioutcaps" />
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MidiOutCaps
    {
        public short mid;
        public short pid;
        public int driverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string name;
        public short technology;
        public short voices;
        public short notes;
        public short channelMask;
        public int support;
    }

    /// <summary>
    /// <see href="https://docs.microsoft.com/en-us/windows/win32/api/mmeapi/ns-mmeapi-midihdr" />
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MidiHeader
    {
        public IntPtr data;
        public int bufferLength;
        public int bytesRecorded;
        public int user;
        public int flags;
        public IntPtr next;
        public int reserved;
        public int offset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public int[] reservedArray;
    }

}
