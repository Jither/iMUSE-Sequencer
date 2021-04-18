using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Verbs
{
    public abstract class Command
    {
        protected Command(CommonOptions options)
        {
            LogProvider.Level = options.LogLevel;
        }

        public abstract void Execute();
    }
}
