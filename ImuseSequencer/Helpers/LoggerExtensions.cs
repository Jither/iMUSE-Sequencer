using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Helpers
{
    public static class LoggerExtensions
    {
        public static void Colored(this Logger logger, string message, string color)
        {
            if (color != null)
            {
                logger.Info($"[{color}]{message}[/]");
            }
            else
            {
                logger.Info(message);
            }
        }
    }
}
