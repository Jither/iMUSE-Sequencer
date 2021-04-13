using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Helpers
{
    public static class StringConverter
    {
        public static string PascalToKebabCase(string str)
        {
            string result = "";
            bool previousWasUpper = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (Char.IsUpper(c))
                {
                    if (i > 0 && !previousWasUpper)
                    {
                        result += "-";
                    }
                    previousWasUpper = true;
                    c = Char.ToLower(c);
                }
                else
                {
                    previousWasUpper = false;
                }
                result += c;
            }
            return result;
        }
    }
}
