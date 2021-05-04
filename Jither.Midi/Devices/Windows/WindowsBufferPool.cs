using Jither.Logging;
using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Devices.Windows
{
    public sealed class WindowsBufferPool : IDisposable
    {
        private sealed class WindowsBuffer : IDisposable
        {
            private readonly GCHandle handle;
            private readonly MidiHeader header;
            private bool disposed;
            public IntPtr HeaderPointer => handle.AddrOfPinnedObject();
            public IntPtr DataPointer => header.lpData;
            public int Size { get; }

            public int DataLength
            {
                get => header.dwBufferLength;
                set
                {
                    header.dwBufferLength = value;
                    header.dwBytesRecorded = value;
                }
            }

            public WindowsBuffer(int size)
            {
                Size = size;
                header = new MidiHeader
                {
                    lpData = Marshal.AllocHGlobal(size),
                    dwFlags = 0
                };

                // Pinning the header for reuse:
                handle = GCHandle.Alloc(header, GCHandleType.Pinned);
            }

            ~WindowsBuffer()
            {
                Dispose(false);
            }

            private void Dispose(bool disposing)
            {
                if (disposed)
                {
                    return;
                }

                if (disposing)
                {
                    // Nothing to do for now
                }

                Marshal.FreeHGlobal(DataPointer);
                handle.Free();

                disposed = true;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public const int LargeSize = 64000;
        public const int SmallSize = 512;

        private static readonly Logger logger = LogProvider.Get(nameof(WindowsBufferPool));
        private readonly ConcurrentBag<WindowsBuffer> smallBuffers = new();
        private readonly ConcurrentBag<WindowsBuffer> largeBuffers = new();
        private readonly ConcurrentDictionary<IntPtr, WindowsBuffer> buffersInUse = new();

        private bool disposed;

        public int UnreleasedBufferCount => buffersInUse.Count;

        public IntPtr Build(SysexMessage message)
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

        public IntPtr Build(byte[] data)
        {
            int length = data.Length;

            if (length > LargeSize)
            {
                throw new ArgumentException($"Size of data is too large for a pooled buffer. Maximum is {LargeSize} bytes - these data are {length} bytes.");
            }

            var buffers = length > SmallSize ? largeBuffers : smallBuffers;
            if (!buffers.TryTake(out var buffer))
            {
                buffer = new WindowsBuffer(length > SmallSize ? LargeSize : SmallSize);
            }
            
            buffersInUse.TryAdd(buffer.HeaderPointer, buffer);

            buffer.DataLength = length;

            Marshal.Copy(data, 0, buffer.DataPointer, length);

            return buffer.HeaderPointer;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            foreach (var buffer in smallBuffers)
            {
                buffer.Dispose();
            }
            foreach (var buffer in largeBuffers)
            {
                buffer.Dispose();
            }
            smallBuffers.Clear();
            largeBuffers.Clear();
        }

        public void Release(IntPtr headerPointer)
        {
            if (!buffersInUse.TryRemove(headerPointer, out var buffer))
            {
                throw new InvalidOperationException("Attempt to release buffer that doesn't exist");
            }
            var buffers = buffer.Size > SmallSize ? largeBuffers : smallBuffers;
            buffers.Add(buffer);
        }
    }
}
