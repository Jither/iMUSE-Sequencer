using Jither.Imuse.Commands;
using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Runtime.Executers;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse
{
    public class QueueItem
    {
        public int SoundId { get; }
        public int MarkerId { get; }

        public IReadOnlyList<CommandCall> Commands { get; }

        public QueueItem(int soundId, int markerId, IReadOnlyList<CommandCall> commands)
        {
            SoundId = soundId;
            MarkerId = markerId;
            this.Commands = commands;
        }
    }

    public class ImuseQueue
    {
        private static readonly Logger logger = LogProvider.Get(nameof(ImuseQueue));
        private readonly Queue<QueueItem> items = new();
        private readonly ImuseEngine engine;

        public ImuseQueue(ImuseEngine engine)
        {
            this.engine = engine;
        }

        public void Enqueue(int soundId, int markerId, List<CommandCall> commands)
        {
            items.Enqueue(new QueueItem(soundId, markerId, commands));
        }

        public bool SoundInQueue(int soundId)
        {
            // TODO: Implement SoundInQueue when queue has been rewritten
            return false;
        }

        public void ProcessMarker(Player player, int markerId)
        {
            if (!items.TryPeek(out var item))
            {
                return;
            }

            if (item.SoundId == player.SoundId && item.MarkerId == markerId)
            {
                logger.Info($"Marker {item.MarkerId} on sound {item.SoundId} triggered in queue");
                items.Dequeue();
                foreach (var cmd in item.Commands)
                {
                    cmd.Command.Execute(cmd.Arguments);
                }
            }
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
