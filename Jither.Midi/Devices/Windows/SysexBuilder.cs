using Jither.Midi.Parsing;
using System;
using System.Runtime.InteropServices;

namespace Jither.Midi.Devices.Windows
{
    internal static class SysexBuilder
    {
        public static IntPtr Build(SysexMessage message)
        {
            byte[] data;
            int length;
            
            if (!message.Continuation)
            {
                length = message.Data.Length + 1;
                data = new byte[length];

                data[0] = 0xf0;
                Array.Copy(message.Data, 0, data, 1, message.Data.Length);
            }
            else
            {
                length = message.Data.Length;
                data = new byte[length];

                Array.Copy(message.Data, data, length);
            }

            var header = new MidiHeader
            {
                bufferLength = length,
                bytesRecorded = length,
                data = Marshal.AllocHGlobal(length),
                flags = 0
            };

            for (int i = 0; i < length; i++)
            {
                Marshal.WriteByte(header.data, i, data[i]);
            }

            IntPtr bufferPointer;
            try
            {
                bufferPointer = Marshal.AllocHGlobal(Marshal.SizeOf<MidiHeader>());
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
                Marshal.FreeHGlobal(header.data);
                throw;
            }
        }

        public static void Destroy(IntPtr bufferPointer)
        {
            MidiHeader header = Marshal.PtrToStructure<MidiHeader>(bufferPointer);
            Marshal.FreeHGlobal(header.data);
            Marshal.FreeHGlobal(bufferPointer);
        }
    }
}
