using Jither.Midi.Parsing;
using Jither.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Jither.Midi.Devices.Windows
{
    public class WindowsOutputDevice : OutputDevice
    {
#pragma warning disable IDE1006 // Naming Styles - keeping case of WinAPI functions

        [DllImport("winmm.dll")]
        private static extern int midiConnect(IntPtr handleA, IntPtr handleB, IntPtr reserved);

        [DllImport("winmm.dll")]
        private static extern int midiDisconnect(IntPtr handleA, IntPtr handleB, IntPtr reserved);

        [DllImport("winmm.dll")]
        private static extern int midiOutOpen(out IntPtr handle, int deviceID, MidiOutProc proc, IntPtr instance, int flags);

        private delegate void MidiOutProc(IntPtr hnd, int msg, IntPtr instance, IntPtr param1, IntPtr param2);

        [DllImport("winmm.dll")]
        private static extern int midiOutClose(IntPtr handle);

        [DllImport("winmm.dll")]
        private static extern int midiOutReset(IntPtr handle);

        [DllImport("winmm.dll")]
        private static extern int midiOutShortMsg(IntPtr handle, int message);

        [DllImport("winmm.dll")]
        private static extern int midiOutPrepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

        [DllImport("winmm.dll")]
        private static extern int midiOutUnprepareHeader(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

        [DllImport("winmm.dll")]
        private static extern int midiOutLongMsg(IntPtr handle, IntPtr headerPtr, int sizeOfMidiHeader);

#pragma warning restore IDE1006 // Naming Styles

        protected static readonly int sizeOfMidiHeader = Marshal.SizeOf<MidiHeader>();

        private IntPtr handle;
        private readonly MessagingSynchronizationContext context = new();
        private readonly object lockMidi = new();
        private int sysexBufferCount = 0;

        public WindowsOutputDevice(int deviceId) : base(deviceId)
        {
            int result = midiOutOpen(out handle, deviceId, HandleMessage, IntPtr.Zero, WinApiConstants.CALLBACK_FUNCTION);
            EnsureSuccess(result);
        }

        ~WindowsOutputDevice()
        {
            Dispose(false);
        }

        private void Connect(IntPtr handleA, IntPtr handleB)
        {
            int result = midiConnect(handleA, handleB, IntPtr.Zero);
            EnsureSuccess(result);
        }

        private void Disconnect(IntPtr handleA, IntPtr handleB)
        {
            int result = midiDisconnect(handleA, handleB, IntPtr.Zero);
            EnsureSuccess(result);
        }

        public void SendRaw(int message)
        {
            lock (lockMidi)
            {
                int result = midiOutShortMsg(handle, message);
                EnsureSuccess(result);
            }
        }

        public void SendMessage(MidiMessage message)
        {
            if (message is SysexMessage sysex)
            {
                SendSysex(sysex);
            }
            else
            {
                SendRaw(message.RawMessage);
            }
        }

        private void SendSysex(SysexMessage message)
        {
            lock (lockMidi)
            {
                IntPtr sysexPointer = SysexBuilder.Build(message);

                try
                {
                    int result = midiOutPrepareHeader(handle, sysexPointer, sizeOfMidiHeader);
                    EnsureSuccess(result);

                    sysexBufferCount++;

                    try
                    {
                        result = midiOutLongMsg(handle, sysexPointer, sizeOfMidiHeader);
                        EnsureSuccess(result);
                    }
                    catch (WindowsMidiDeviceException)
                    {
                        // We already got an exception which is more important, so throw out errors during cleanup
                        _ = midiOutUnprepareHeader(handle, sysexPointer, sizeOfMidiHeader);
                        sysexBufferCount--;
                        throw;
                    }

                }
                catch (WindowsMidiDeviceException)
                {
                    SysexBuilder.Destroy(sysexPointer);
                    throw;
                }
            }
        }

        private void HandleMessage(IntPtr handle, int message, IntPtr instance, IntPtr param1, IntPtr param2)
        {
            switch (message)
            {
                case WinApiConstants.MOM_OPEN:
                    break;
                case WinApiConstants.MOM_CLOSE:
                    break;
                case WinApiConstants.MOM_DONE:
                    context.Post(ReleaseBuffer, param1);
                    break;
            }
        }

        private void ReleaseBuffer(object state)
        {
            lock (lockMidi)
            {
                IntPtr sysexPointer = (IntPtr)state;

                // Unprepare the buffer.
                int result = midiOutUnprepareHeader(handle, sysexPointer, sizeOfMidiHeader);

                EnsureSuccess(result);

                // Release the buffer resources.
                SysexBuilder.Destroy(sysexPointer);

                sysexBufferCount--;

                Monitor.Pulse(lockMidi);

                Debug.Assert(sysexBufferCount >= 0);
            }
        }

        public override void Reset()
        {
            lock (lockMidi)
            {
                int result = midiOutReset(handle);
                EnsureSuccess(result);
                while (sysexBufferCount > 0)
                {
                    Monitor.Wait(lockMidi);
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

        protected override void Dispose(bool disposing)
        {
            try
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
                        int result = midiOutClose(handle);
                        EnsureSuccess(result);
                    }
                }
                else
                {
                    // We can't do much about error conditions in these calls.
                    _ = midiOutReset(handle);
                    _ = midiOutClose(handle);
                }
                handle = IntPtr.Zero;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
