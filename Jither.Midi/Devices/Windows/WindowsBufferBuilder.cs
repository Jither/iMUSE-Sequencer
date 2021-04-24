using Jither.Midi.Messages;
using Jither.Midi.Files;
using System;
using System.Runtime.InteropServices;
using Jither.Midi.Helpers;

namespace Jither.Midi.Devices.Windows
{
    internal static class WindowsBufferBuilder
    {
        public static IntPtr Build(SysexMessage message)
        {
            byte[] data;

            if (!message.Continuation)
            {
                int length = message.Data.Length + 1;
                data = new byte[length];

                data[0] = 0xf0;
                Array.Copy(message.Data, 0, data, 1, message.Data.Length);
            }
            else
            {
                data = message.Data;
            }

            return Build(data);
        }

        public static IntPtr Build(byte[] data)
        {
            int length = data.Length;
            var header = new MidiHeader
            {
                dwBufferLength = length,
                dwBytesRecorded = length,
                lpData = Marshal.AllocHGlobal(length),
                dwFlags = 0
            };

            Marshal.Copy(data, 0, header.lpData, length);

            try
            {
                var bufferPointer = Marshal.AllocHGlobal(Marshal.SizeOf<MidiHeader>());
                try
                {
                    Marshal.StructureToPtr(header, bufferPointer, false);
                    return bufferPointer;
                }
                catch
                {
                    Marshal.FreeHGlobal(bufferPointer);
                    throw;
                }
            }
            catch
            {
                Marshal.FreeHGlobal(header.lpData);
                throw;
            }
        }

        public static void Destroy(IntPtr bufferPointer)
        {
            MidiHeader header = Marshal.PtrToStructure<MidiHeader>(bufferPointer);
            Marshal.FreeHGlobal(header.lpData);
            Marshal.FreeHGlobal(bufferPointer);
        }
    }
}
