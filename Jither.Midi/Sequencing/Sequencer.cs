using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Sequencing
{
    public abstract class Sequencer : IDisposable
    {
        private readonly Clock clock;
        private bool isPlaying;
        private bool disposed;

        public int Position
        {
            get => clock.Ticks;
            set
            {
                bool wasPlaying = isPlaying;
                Stop();
                clock.SetTicks(value);

                if (wasPlaying)
                {
                    Resume();
                }
            }
        }

        protected Sequencer(Clock clock)
        {
            this.clock = clock;
            clock.Tick += Clock_Tick;
        }

        public void Start()
        {
            Stop();
            Position = 0;
            Resume();
        }

        public void Resume()
        {
            Stop();

            isPlaying = true;
            // TODO: Set PPQN from file
            clock.Resume();
        }

        public void Stop()
        {
            if (!isPlaying)
            {
                return;
            }

            isPlaying = false;
            clock.Stop();
            // TODO: All sounds off
        }

        protected abstract void Tick();

        protected void SetTempo(int tempo)
        {
            clock.Tempo = tempo;
        }

        private void Clock_Tick()
        {
            if (!isPlaying)
            {
                return;
            }
            Tick();
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

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                clock.Dispose();
            }
        }
    }
}
