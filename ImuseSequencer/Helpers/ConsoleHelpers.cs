using Jither.Imuse;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Helpers
{
    public static class ConsoleHelpers
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ConsoleHelpers));

        private static ConsoleCancelEventHandler cancelHandler;

        public static void SetupCancelHandler(ImuseEngine engine, ITransmitter transmitter)
        {
            // Clean up, even with Ctrl+C
            cancelHandler = new ConsoleCancelEventHandler((sender, e) =>
            {
                logger.Warning("Abrupt exit - trying to clean up...");
                engine.Dispose();
                if (transmitter is IDisposable disposableTransmitter)
                {
                    disposableTransmitter.Dispose();
                }
            });
            Console.CancelKeyPress += cancelHandler;
        }

        public static void TearDownCancelHandler()
        {
            if (cancelHandler != null)
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }

    }
}
