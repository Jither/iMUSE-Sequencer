using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Helpers
{
    public static class ByteArrayExtensions
    {
        public static string ToHex(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace('-', ' ').ToLower();
        }
    }
}
