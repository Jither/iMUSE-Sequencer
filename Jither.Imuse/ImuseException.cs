using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse
{
    public class ImuseException : Exception
    {
        public ImuseException(string message) : base(message)
        {
        }
    }
}
