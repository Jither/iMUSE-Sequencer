using Jither.Midi.Messages;
using Jither.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Jither.Midi.Devices.Windows
{
    public class WindowsOutputDevice : OutputDevice
    {
        private IntPtr handle;
        private readonly MessagingSynchronizationContext context = new();
        private readonly object lockMidi = new();
        private int sysexBufferCount = 0;
        private readonly WinApi.MidiOutProc midiOutCallback;

        public WindowsOutputDevice(int deviceId) : base(deviceId)
        {
            // We need to manually create the delegate to avoid it being garbage collected
            midiOutCallback = new WinApi.MidiOutProc(HandleMessage);
            int result = WinApi.midiOutOpen(out handle, deviceId, midiOutCallback, IntPtr.Zero, WinApiConstants.CALLBACK_FUNCTION);
            EnsureSuccess(result);

            // TODO: Actually start context
            //context.Start();
        }

        ~WindowsOutputDevice()
        {
            Dispose(false);
        }

        public override void SendRaw(int message)
        {
            lock (lockMidi)
            {
                int result = WinApi.midiOutShortMsg(handle, message);
                EnsureSuccess(result);
            }
        }

        public override void SendMessage(MidiMessage message)
        {
            if (message is MetaMessage)
            {
                // Don't send meta messages
                return;
            }

            if (message is SysexMessage sysex)
            {
                SendSysex(sysex);
            }
            else
            {
                SendRaw(message.RawMessage);
            }
        }

        public override void Reset()
        {
            lock (lockMidi)
            {
                int result = WinApi.midiOutReset(handle);
                EnsureSuccess(result);
            }
        }

        private void SendSysex(SysexMessage message)
        {
            lock (lockMidi)
            {
                IntPtr sysexPointer = WindowsBufferBuilder.Build(message);

                try
                {
                    int result = WinApi.midiOutPrepareHeader(handle, sysexPointer, WinApi.SizeOfMidiHeader);
                    EnsureSuccess(result);

                    sysexBufferCount++;

                    try
                    {
                        result = WinApi.midiOutLongMsg(handle, sysexPointer, WinApi.SizeOfMidiHeader);
                        EnsureSuccess(result);
                    }
                    catch (WindowsMidiDeviceException)
                    {
                        // We already got an exception which is more important, so throw out errors during cleanup
                        _ = WinApi.midiOutUnprepareHeader(handle, sysexPointer, WinApi.SizeOfMidiHeader);
                        sysexBufferCount--;
                        throw;
                    }

                }
                catch (WindowsMidiDeviceException)
                {
                    WindowsBufferBuilder.Destroy(sysexPointer);
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
                int result = WinApi.midiOutUnprepareHeader(handle, sysexPointer, WinApi.SizeOfMidiHeader);

                EnsureSuccess(result);

                // Release the buffer resources.
                WindowsBufferBuilder.Destroy(sysexPointer);

                sysexBufferCount--;

                Monitor.Pulse(lockMidi);

                Debug.Assert(sysexBufferCount >= 0);
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
                        int result = WinApi.midiOutClose(handle);
                        EnsureSuccess(result);
                        // TODO: Reinstate this, when context is synchronizing
                        /*
                        while (sysexBufferCount > 0)
                        {
                            Monitor.Wait(lockMidi);
                        }
                        */
                    }
                }
                else
                {
                    // We can't do much about error conditions in these calls.
                    _ = WinApi.midiOutReset(handle);
                    _ = WinApi.midiOutClose(handle);
                }
                handle = IntPtr.Zero;
                //context.Stop();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
