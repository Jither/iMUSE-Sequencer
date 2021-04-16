using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Messages
{
    public class MidiMessageException : Exception
    {
        public MidiMessageException(string message) : base(message)
        {
        }
    }
}
