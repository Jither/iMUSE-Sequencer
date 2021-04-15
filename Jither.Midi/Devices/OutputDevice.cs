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

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract void Reset();

        public void Close()
        {
            if (disposed)
            {
                return;
            }
            Dispose(true);
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
