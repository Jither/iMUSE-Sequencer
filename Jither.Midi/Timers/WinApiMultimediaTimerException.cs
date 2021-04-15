using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Timers
{
    public class WinApiMultimediaTimerException : TimerException
    {
        public const int TIMERR_NOERROR = 0;

        public WinApiMultimediaTimerException(int error) : base(GetMessage(error))
        {

        }

        private static string GetMessage(int error)
        {
            return $"Timer call failed. Error code: {error}";
        }
    }
}
