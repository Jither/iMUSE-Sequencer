using Jither.Midi.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Drivers
{
    public abstract class Driver
    {
        protected readonly OutputDevice output;

        protected Driver(OutputDevice output)
        {
            this.output = output;
        }

        public abstract void Init();
        public abstract void Reset();
    }
}
