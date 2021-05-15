using Jither.Midi.Messages;
using System;

namespace Jither.Midi.Devices
{
    // TODO: Generalize OutputStream for cross-platform
    public abstract class OutputStream : IDisposable
    {
        private bool disposed;

        public string Name { get; }
        public abstract int Division { get; set; }
        public abstract int Tempo { get; set; }

        public abstract event Action<int> NoOpOccurred;

        protected OutputStream(string name)
        {
            Name = name;
        }

        public abstract void Play();
        public abstract void Pause();
        public abstract void Reset();
        public abstract void Stop();
        public abstract void Write(MidiEvent e);
        public abstract void WriteNoOp(long ticks, int data);
        public abstract void Flush();

        protected abstract void Dispose(bool disposing);

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