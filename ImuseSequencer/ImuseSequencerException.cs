using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer
{
    public class ImuseSequencerException : Exception
    {
        public ImuseSequencerException(string message) : base(message)
        {
        }

        public ImuseSequencerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
