using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse.Commands
{
    public class MuskCommands
    {
        private static readonly Random rng = new();

        private static readonly Logger logger = LogProvider.Get(nameof(ImuseCommands));

        public void PrintLine(string line)
        {
            logger.Info(line);
        }

        public int Random(int min, int max)
        {
            // Unlike C#, random upper is inclusive
            return rng.Next(min, max + 1);
        }
    }
}
