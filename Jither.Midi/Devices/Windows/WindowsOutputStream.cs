using Jither.Logging;
using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jither.Midi.Devices.Windows
{
    public struct Message
    {
        public Action<object> Callback { get; }
        public object State { get; }

        public Message(Action<object> callback, object state)
        {
            Callback = callback;
            State = state;
        }
    }

    // TODO: Proper threading!!!
    public class WindowsOutputStream : IDisposable
    {
        private static readonly Logger logger = LogProvider.Get(nameof(WindowsOutputStream));
        private readonly WinApi.MidiOutProc midiOutCallback;
        private IntPtr handle;
        private readonly object lockMidi = new();
        private bool disposed;
        private readonly MemoryStream eventsStream = new();
        private readonly WindowsMidiStreamWriter writer;
        private readonly Channel<Message> messageChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        private const int eventCodeOffset = 8;
        private const int eventTypeIndex = 11;
        private readonly uint streamId = 0; // Actually never changes - it's not used by WinAPI.
        private readonly WindowsBufferPool bufferPool = new();
        private bool closing;

        public event Action<int> NoOpOccurred; 

        public WindowsOutputStream(int deviceId)
        {
            // We need to manually create the delegate to avoid it being garbage collected
            midiOutCallback = new WinApi.MidiOutProc(HandleMessage);
            int result = WinApi.midiStreamOpen(out handle, ref deviceId, 1, midiOutCallback, IntPtr.Zero, WinApiConstants.CALLBACK_FUNCTION);

            EnsureSuccess(result);

            writer = new WindowsMidiStreamWriter(eventsStream);
        }

        ~WindowsOutputStream()
        {
            Dispose(false);
        }

        public async Task Run()
        {
            await foreach (Message item in messageChannel.Reader.ReadAllAsync())
            {
                item.Callback(item.State);
            }
            logger.Debug("OutputStream finished");
        }

        public void Start()
        {
            lock (lockMidi)
            {
                int result = WinApi.midiStreamRestart(handle);

                EnsureSuccess(result);
            }
        }

        public void Pause()
        {
            lock (lockMidi)
            {
                int result = WinApi.midiStreamPause(handle);

                EnsureSuccess(result);
            }
        }

        public void Stop()
        {
            lock (lockMidi)
            {
                int result = WinApi.midiStreamStop(handle);

                EnsureSuccess(result);
            }
        }

        public void Reset()
        {
            eventsStream.SetLength(0);

            int result = WinApi.midiOutReset(handle);
            EnsureSuccess(result);
        }

        public void Write(MidiEvent e)
        {
            Write(e.AbsoluteTicks, e.Message);
        }

        private void Write(long ticks, MidiMessage message)
        {
            if (message is MetaMessage and not SetTempoMessage)
            {
                return;
            }

            writer.WriteHeader(ticks, streamId);
            message.Write(writer);
            // Quick fix: Stream buffers may not exceed 64K. 64000 bytes should have enough safety margin
            // (not going to get a single MIDI message with 1536 bytes)
            if (eventsStream.Length >= WindowsBufferPool.LargeSize)
            {
                Flush();
            }
        }

        public void WriteNoOp(long ticks, int data)
        {
            writer.WriteHeader(ticks, streamId);
            writer.WriteNoOp(data);

            // Quick fix: Stream buffers may not exceed 64K. 64000 bytes should have enough safety margin
            // (not going to get a single MIDI message with 1536 bytes)
            if (eventsStream.Length >= WindowsBufferPool.LargeSize)
            {
                Flush();
            }
        }

        public void Flush()
        {
            lock (lockMidi)
            {
                if (closing)
                {
                    return;
                }
                eventsStream.Flush();
                var data = eventsStream.ToArray();
                if (data.Length == 0)
                {
                    return;
                }
                var headerPointer = bufferPool.Build(data);

                eventsStream.SetLength(0);

                int result = WinApi.midiOutPrepareHeader(handle, headerPointer, WinApi.SizeOfMidiHeader);

                EnsureSuccess(result);

                try
                {
                    result = WinApi.midiStreamOut(handle, headerPointer, WinApi.SizeOfMidiHeader);
                    EnsureSuccess(result);
                }
                catch (WindowsMidiDeviceException)
                {
                    result = WinApi.midiOutUnprepareHeader(handle, headerPointer, WinApi.SizeOfMidiHeader);
                    EnsureSuccess(result);
                    bufferPool.Release(headerPointer);
                    throw;
                }
            }
        }

        public MMTime GetTime(TimeType type)
        {
            var time = new MMTime
            {
                type = (int)type
            };

            lock (lockMidi)
            {
                int result = WinApi.midiStreamPosition(handle, ref time, Marshal.SizeOf<MMTime>());
                EnsureSuccess(result);
            }

            return time;
        }

        private void HandleMessage(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            switch (msg)
            {
                case WinApiConstants.MOM_OPEN:
                    break;
                case WinApiConstants.MOM_CLOSE:
                    break;
                case WinApiConstants.MOM_POSITIONCB:
                    messageChannel.Writer.TryWrite(new Message(HandleNoOp, param1));
                    break;
                case WinApiConstants.MOM_DONE:
                    messageChannel.Writer.TryWrite(new Message(ReleaseBuffer, param1));
                    break;
            }
        }

        private void HandleNoOp(object state)
        {
            try
            {
                // TODO: Should invoke on the thread that created this Stream.
                // TODO: Right now, we're not actually checking the bits of the NoOp.
                // dwOffset in the header seems useless (it's always 0 or 1(???), in spite of the buffer being large).
                NoOpOccurred?.Invoke(0);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                throw;
            }
        }

        private void ReleaseBuffer(object state)
        {
            lock (lockMidi)
            {
                IntPtr bufferPointer = (IntPtr)state;

                try
                {
                    // Unprepare the buffer.
                    int result = WinApi.midiOutUnprepareHeader(handle, bufferPointer, WinApi.SizeOfMidiHeader);
                    EnsureSuccess(result);

                    // Release the buffer resources.
                    bufferPool.Release(bufferPointer);
                }
                catch (Exception ex)
                {
                    logger.Error(ex.Message);
                }
            }
        }

        public int Division
        {
            get
            {
                var d = new Property
                {
                    cbStruct = WinApi.SizeOfProperty
                };

                lock (lockMidi)
                {
                    int result = WinApi.midiStreamProperty(handle, ref d, WinApiConstants.MIDIPROP_GET | WinApiConstants.MIDIPROP_TIMEDIV);
                    EnsureSuccess(result);
                }

                return d.property;
            }
            set
            {
                if (value < 24)
                {
                    throw new ArgumentOutOfRangeException(nameof(Division), value, "PPQN should be >= 24.");
                }

                var d = new Property
                {
                    cbStruct = WinApi.SizeOfProperty,
                    property = value
                };

                lock (lockMidi)
                {
                    int result = WinApi.midiStreamProperty(handle, ref d, WinApiConstants.MIDIPROP_SET | WinApiConstants.MIDIPROP_TIMEDIV);
                    EnsureSuccess(result);
                }
            }
        }

        public int Tempo
        {
            get
            {
                var t = new Property
                {
                    cbStruct = WinApi.SizeOfProperty
                };

                lock (lockMidi)
                {
                    int result = WinApi.midiStreamProperty(handle, ref t, WinApiConstants.MIDIPROP_GET | WinApiConstants.MIDIPROP_TEMPO);

                    EnsureSuccess(result);
                }

                return t.property;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Tempo), value, "Tempo should be a positive integer");
                }

                var t = new Property
                {
                    cbStruct = WinApi.SizeOfProperty,
                    property = value
                };

                lock (lockMidi)
                {
                    int result = WinApi.midiStreamProperty(handle, ref t, WinApiConstants.MIDIPROP_SET | WinApiConstants.MIDIPROP_TEMPO);

                    EnsureSuccess(result);
                }
            }
        }

        private static void EnsureSuccess(int result)
        {
            if (result != WindowsMidiDeviceException.MMSYSERR_NOERROR)
            {
                throw new WindowsMidiDeviceException(result);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (lockMidi)
                {
                    // Make sure no more buffers are allocated when lockMidi is released
                    closing = true;
                    try
                    {
                        Reset();
                    }
                    catch (WindowsMidiDeviceException)
                    {
                        // e.g. Munt does not support resetting
                    }

                    // Wait for remaining buffers to be released:
                    while (bufferPool.UnreleasedBufferCount > 0)
                    {
                        Monitor.Wait(lockMidi);
                    }
                    messageChannel.Writer.TryComplete();

                    int result = WinApi.midiStreamClose(handle);
                    EnsureSuccess(result);
                }
                bufferPool.Dispose();
                eventsStream.Dispose();
            }
            else
            {
                // We can't do much about error conditions in these calls.
                _ = WinApi.midiOutReset(handle);
                _ = WinApi.midiStreamClose(handle);
            }
            handle = IntPtr.Zero;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
