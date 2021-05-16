using Jither.Logging;
using Jither.Midi.Messages;
using Jither.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jither.Midi.Devices.Windows
{
    public class WindowsOutputDevice : OutputDevice
    {
        private static readonly Logger logger = LogProvider.Get(nameof(WindowsOutputStream));

        private IntPtr handle;
        private readonly Channel<Message> messageChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        private readonly object lockMidi = new();
        private readonly WinApi.MidiOutProc midiOutCallback;
        private readonly WindowsBufferPool bufferPool = new();

        public WindowsOutputDevice(int deviceId, string name) : base(name)
        {
            // We need to manually create the delegate to avoid it being garbage collected
            midiOutCallback = new WinApi.MidiOutProc(HandleMessage);
            int result = WinApi.midiOutOpen(out handle, deviceId, midiOutCallback, IntPtr.Zero, WinApiConstants.CALLBACK_FUNCTION);
            EnsureSuccess(result);

            Task.Run(Run);
        }

        ~WindowsOutputDevice()
        {
            Dispose(false);
        }

        private async Task Run()
        {
            await foreach (Message item in messageChannel.Reader.ReadAllAsync())
            {
                item.Callback(item.State);
            }
            logger.Debug("OutputDevice finished");
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
                var headerPointer = bufferPool.Build(message);

                try
                {
                    int result = WinApi.midiOutPrepareHeader(handle, headerPointer, WinApi.SizeOfMidiHeader);
                    EnsureSuccess(result);

                    try
                    {
                        result = WinApi.midiOutLongMsg(handle, headerPointer, WinApi.SizeOfMidiHeader);
                        EnsureSuccess(result);
                    }
                    catch (WindowsMidiDeviceException)
                    {
                        // We already got an exception which is more important, so throw out errors during cleanup
                        _ = WinApi.midiOutUnprepareHeader(handle, headerPointer, WinApi.SizeOfMidiHeader);
                        throw;
                    }

                }
                catch (WindowsMidiDeviceException)
                {
                    bufferPool.Release(headerPointer);
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
                    messageChannel.Writer.TryWrite(new Message(ReleaseBuffer, param1));
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
                bufferPool.Release(sysexPointer);
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

                        while (bufferPool.UnreleasedBufferCount > 0)
                        {
                            if (!Monitor.Wait(lockMidi, 2000))
                            {
                                logger.Warning("MIDI buffer release deadlock timeout");
                                break;
                            }
                        }
                        messageChannel.Writer.TryComplete();

                        int result = WinApi.midiOutClose(handle);
                        EnsureSuccess(result);
                    }
                    bufferPool.Dispose();
                }
                else
                {
                    // We can't do much about error conditions in these calls.
                    _ = WinApi.midiOutReset(handle);
                    _ = WinApi.midiOutClose(handle);
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
