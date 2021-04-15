using Jither.Midi.Messages;
using System;

namespace Jither.Midi.Devices
{
    public abstract class OutputDevice : IDisposable
    {
        private bool disposed = false;

        public int DeviceId { get; }

        public OutputDevice(int deviceId)
        {
            DeviceId = deviceId;
        }

        public abstract void SendMessage(MidiMessage message);
        public abstract void SendRaw(int message);
        public abstract void Reset();

        protected virtual void Dispose(bool disposing)
        {
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
