using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Scripting.Types
{
    // TODO: Handle measures! Right now, everything using Time pretends measures don't exist.
    public record Time(int Measure, int Beat, int Tick)
    {
        public static bool TryParse(string str, out Time time)
        {
            time = null;
            var parts = str.Split(".");
            string strMeasure = "0", strBeat, strTick;
            switch (parts.Length)
            {
                case 2:
                    strBeat = parts[0];
                    strTick = parts[1];
                    break;
                case 3:
                    strMeasure = parts[0];
                    strBeat = parts[1];
                    strTick = parts[2];
                    break;
                default:
                    return false;
            }

            if (!int.TryParse(strMeasure, out int measure))
            {
                return false;
            }
            if (!int.TryParse(strBeat, out int beat))
            {
                return false;
            }
            if (!int.TryParse(strTick, out int tick))
            {
                return false;
            }

            time = new Time(measure, beat, tick);
            return true;
        }

        public override string ToString()
        {
            return $"{Measure}.{Beat}.{Tick:000}";
        }
    }
}
