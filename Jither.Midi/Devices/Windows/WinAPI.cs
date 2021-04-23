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

        public const uint MIDIPROP_SET = 0x80000000;
        public const uint MIDIPROP_GET = 0x40000000;
        public const uint MIDIPROP_TIMEDIV = 0x00000001;
        public const uint MIDIPROP_TEMPO = 0x00000002;

        public const byte MEVT_CALLBACK = 0x40;

        public const byte MEVT_SHORTMSG = 0x00;
        public const byte MEVT_TEMPO = 0x01;
        public const byte MEVT_NOP = 0x02;
        public const byte MEVT_LONGMSG = 0x80;
        public const byte MEVT_COMMENT = 0x82;
        public const byte MEVT_VERSION = 0x84;

        public const int MOM_POSITIONCB = 0x3CA;

        public const int MHDR_NONE = 0x00;
        public const int MHDR_DONE = 0x01;
        public const int MHDR_PREPARED = 0x02;
        public const int MHDR_INQUEUE = 0x04;
        public const int MHDR_ISSTRM = 0x08;
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
        public IntPtr lpData;
        public int dwBufferLength;
        public int dwBytesRecorded;
        public IntPtr dwUser;
        public int dwFlags;
        public IntPtr lpNext;
        public IntPtr reserved;
        public int dwOffset;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public IntPtr[] dwReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Property
    {
        public int sizeOfProperty;
        public int property;
    }

    public enum TimeType
    {
        Milliseconds = 0x0001,
        Samples = 0x0002,
        Bytes = 0x0004,
        Smpte = 0x0008,
        Midi = 0x0010,
        Ticks = 0x0020
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct MMTime
    {
        [FieldOffset(0)]
        public int type;

        [FieldOffset(4)]
        public int milliseconds;

        [FieldOffset(4)]
        public int samples;

        [FieldOffset(4)]
        public int byteCount;

        [FieldOffset(4)]
        public int ticks;

        [FieldOffset(4)]
        public byte hours;

        [FieldOffset(5)]
        public byte minutes;

        [FieldOffset(6)]
        public byte seconds;

        [FieldOffset(7)]
        public byte frames;

        [FieldOffset(8)]
        public byte framesPerSecond;

        [FieldOffset(9)]
        public byte dummy;

        [FieldOffset(10)]
        public byte pad1;

        [FieldOffset(11)]
        public byte pad2;

        [FieldOffset(4)]
        public int songPositionPointer;
    }

    public static class WinApi
    {
        public static readonly int SizeOfMidiHeader = Marshal.SizeOf<MidiHeader>();
        public static readonly int SizeOfMidiEvent = 12;

        public delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

#pragma warning disable IDE1006 // Naming Styles - keeping case of WinAPI functions

        [DllImport("winmm.dll")]
        public static extern int midiOutGetNumDevs();
        [DllImport("winmm.dll")]
        public static extern int midiOutGetDevCaps(IntPtr deviceId, ref MidiOutCaps caps, int sizeOfMidiOutCaps);

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        public static extern int midiOutGetErrorText(int errCode, StringBuilder message, int sizeOfMessage);

        [DllImport("winmm.dll")]
        public static extern int midiOutOpen(out IntPtr handle, int deviceID, MidiOutProc proc, IntPtr instance, int flags);
        [DllImport("winmm.dll")]
        public static extern int midiOutClose(IntPtr handle);
        [DllImport("winmm.dll")]
        public static extern int midiOutReset(IntPtr handle);
        [DllImport("winmm.dll")]
        public static extern int midiOutShortMsg(IntPtr handle, int message);
        [DllImport("winmm.dll")]
        public static extern int midiOutPrepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);
        [DllImport("winmm.dll")]
        public static extern int midiOutUnprepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);
        [DllImport("winmm.dll")]
        public static extern int midiOutLongMsg(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

        [DllImport("winmm.dll")]
        public static extern int midiStreamOpen(ref IntPtr handle, ref int deviceID, int reserved, MidiOutProc proc, IntPtr instance, uint flag);
        [DllImport("winmm.dll")]
        public static extern int midiStreamClose(IntPtr handle);
        [DllImport("winmm.dll")]
        public static extern int midiStreamOut(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);
        [DllImport("winmm.dll")]
        public static extern int midiStreamPause(IntPtr handle);
        [DllImport("winmm.dll")]
        public static extern int midiStreamPosition(IntPtr handle, ref MMTime t, int sizeOfTime);
        [DllImport("winmm.dll")]
        public static extern int midiStreamProperty(IntPtr handle, ref Property p, uint flags);
        [DllImport("winmm.dll")]
        public static extern int midiStreamRestart(IntPtr handle);
        [DllImport("winmm.dll")]
        public static extern int midiStreamStop(IntPtr handle);

#pragma warning restore IDE1006 // Naming Styles
    }
}
