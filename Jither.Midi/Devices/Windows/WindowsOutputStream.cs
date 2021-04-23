using Jither.Logging;
using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using Jither.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jither.Midi.Devices.Windows
{
    public class MidiStreamWriter
    {
        private readonly Stream stream;
        private readonly byte[] buffer = new byte[4];

        private uint previousTicks;

        public uint PreviousTicks => previousTicks;

        public MidiStreamWriter(Stream stream)
        {
            this.stream = stream;
        }

        public void WriteHeader(long ticks, uint streamId)
        {
            // Less than 0 indicates immediate
            uint deltaTime = ticks >= 0 ? (uint)ticks - previousTicks : previousTicks;
            WriteUint32(deltaTime);
            WriteUint32(streamId);
            if (ticks > previousTicks)
            {
                previousTicks = (uint)ticks;
            }
        }

        public void WriteEvent(int value, int flags)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)((value >> 24) | flags);
            stream.Write(buffer, 0, 4);
        }

        public void WriteShortMessage(byte status, byte value1, byte value2)
        {
            buffer[0] = status;
            buffer[1] = value1;
            buffer[2] = value2;
            buffer[3] = WinApiConstants.MEVT_SHORTMSG;
            stream.Write(buffer, 0, 4);
        }

        public void WriteTempo(int tempo)
        {
            WriteEvent(tempo, WinApiConstants.MEVT_TEMPO);
        }

        public void WriteLongMessage(byte status, byte[] data)
        {
            int fullLength = data.Length + 1;
            WriteEvent(fullLength, WinApiConstants.MEVT_LONGMSG);

            stream.WriteByte(status);
            stream.Write(data, 0, data.Length);

            int padding = 4 - (fullLength % 4);
            for (int i = 0; i < padding; i++)
            {
                stream.WriteByte(0);
            }
        }

        public void WriteNoOp(int data)
        {
            WriteEvent(data, WinApiConstants.MEVT_NOP | WinApiConstants.MEVT_CALLBACK);
        }

        private void WriteUint32(uint value)
        {
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            stream.Write(buffer, 0, 4);
        }
    }

    public class WindowsOutputStream
    {
        private readonly WinApi.MidiOutProc midiOutCallback;
        private IntPtr handle;
        private readonly object lockMidi = new();
        private bool disposed;
        private int bufferCount = 0;
        private readonly MemoryStream eventsStream = new MemoryStream();
        private readonly MidiStreamWriter writer;
        private readonly MessagingSynchronizationContext context = new();
        private const int eventCodeOffset = 8;
        private const int eventTypeIndex = 11;
        private readonly uint streamId = 0; // Actually never changes - it's not used by WinAPI.

        public event Action<int> NoOpOccurred; 

        public WindowsOutputStream(int deviceId)
        {
            // We need to manually create the delegate to avoid it being garbage collected
            midiOutCallback = new WinApi.MidiOutProc(HandleMessage);
            int result = WinApi.midiStreamOpen(ref handle, ref deviceId, 1, midiOutCallback, IntPtr.Zero, WinApiConstants.CALLBACK_FUNCTION);

            EnsureSuccess(result);

            writer = new MidiStreamWriter(eventsStream);

            // TODO: Actually start context
        }

        ~WindowsOutputStream()
        {
            Dispose(false);
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

            //if (message is SysexMessage) return;

            writer.WriteHeader(ticks, streamId);
            message.Write(writer);
            // Quick fix: Stream buffers may not exceed 64K. 64000 bytes should have enough safety margin
            // (not going to get a single MIDI message with 1536 bytes)
            if (eventsStream.Length >= 64000)
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
            if (eventsStream.Length >= 64000)
            {
                Flush();
            }
        }

        public void Flush()
        {
            lock (lockMidi)
            {
                eventsStream.Flush();
                var data = eventsStream.ToArray();
                IntPtr bufferPointer = WindowsBufferBuilder.Build(data);

                eventsStream.SetLength(0);

                int result = WinApi.midiOutPrepareHeader(handle, bufferPointer, WinApi.SizeOfMidiHeader);

                EnsureSuccess(result);

                bufferCount++;

                try
                {
                    result = WinApi.midiStreamOut(handle, bufferPointer, WinApi.SizeOfMidiHeader);
                }
                catch (WindowsMidiDeviceException)
                {
                    WinApi.midiOutUnprepareHeader(handle, bufferPointer, WinApi.SizeOfMidiHeader);
                    bufferCount--;
                    throw;
                }
            }
        }

        public MMTime GetTime(TimeType type)
        {
            MMTime time = new MMTime();

            time.type = (int)type;

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
                    context.Post(HandleNoOp, param1);
                    break;
                case WinApiConstants.MOM_DONE:
                    context.Post(ReleaseBuffer, param1);
                    break;
            }
        }

        private void HandleNoOp(object state)
        {
            IntPtr headerPtr = (IntPtr)state;
            MidiHeader header = Marshal.PtrToStructure<MidiHeader>(headerPtr);

            byte[] midiEvent = new byte[WinApi.SizeOfMidiEvent];

            Marshal.Copy(header.lpData, midiEvent, header.dwOffset, midiEvent.Length);

            // If this is a NoOp event.
            if ((midiEvent[eventTypeIndex] & WinApiConstants.MEVT_NOP) == WinApiConstants.MEVT_NOP)
            {
                // Clear the event type byte.
                midiEvent[eventTypeIndex] = 0;

                int code = BitConverter.ToInt32(midiEvent, eventCodeOffset);

                NoOpOccurred?.Invoke(code);
            }
        }

        private void ReleaseBuffer(object state)
        {
            lock (lockMidi)
            {
                IntPtr bufferPointer = (IntPtr)state;

                // Unprepare the buffer.
                int result = WinApi.midiOutUnprepareHeader(handle, bufferPointer, WinApi.SizeOfMidiHeader);

                EnsureSuccess(result);

                // Release the buffer resources.
                WindowsBufferBuilder.Destroy(bufferPointer);

                bufferCount--;

                Monitor.Pulse(lockMidi);

                Debug.Assert(bufferCount >= 0);
            }
        }



        public int Division
        {
            get
            {
                Property d = new Property();

                d.sizeOfProperty = Marshal.SizeOf<Property>();

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

                Property d = new Property();

                d.sizeOfProperty = Marshal.SizeOf(typeof(Property));
                d.property = value;

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
                Property t = new Property();
                t.sizeOfProperty = Marshal.SizeOf(typeof(Property));

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

                Property t = new Property();
                t.sizeOfProperty = Marshal.SizeOf(typeof(Property));
                t.property = value;

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
                    try
                    {
                        Reset();
                    }
                    catch (WindowsMidiDeviceException)
                    {
                        // e.g. Munt does not support resetting
                    }
                    int result = WinApi.midiStreamClose(handle);
                    EnsureSuccess(result);

                    // TODO: Reinstate this when context is synchronizing:
                    /*
                    while (bufferCount > 0)
                    {
                        Monitor.Wait(lockMidi);
                    }
                    */
                }
                eventsStream.Dispose();
                //context.Stop();
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
