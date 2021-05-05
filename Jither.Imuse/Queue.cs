using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse
{
    public abstract class QueueCommand
    {
        public abstract void Execute(ImuseCommands commands);
    }

    public class JumpCommand : QueueCommand
    {
        public int SoundId { get; }
        public int Track { get; }
        public int Beat { get; }
        public int Tick { get; }

        public JumpCommand(int soundId, int track, int beat, int tick)
        {
            SoundId = soundId;
            Track = track;
            Beat = beat;
            Tick = tick;
        }

        public override void Execute(ImuseCommands commands)
        {
            var player = commands.GetPlayer(SoundId);
            player.Sequencer.Jump(Track, Beat, Tick, "marker");
        }
    }

    public class StartSoundCommand : QueueCommand
    {
        public int SoundId { get; }

        public StartSoundCommand(int soundId)
        {
            SoundId = soundId;
        }

        public override void Execute(ImuseCommands commands)
        {
            commands.StartSound(SoundId);
        }
    }

    public class StopSoundCommand : QueueCommand
    {
        public int SoundId { get; }

        public StopSoundCommand(int soundId)
        {
            SoundId = soundId;
        }

        public override void Execute(ImuseCommands commands)
        {
            commands.StopSound(SoundId);
        }
    }

    public class SetHookCommand : QueueCommand
    {
        public int SoundId { get; }
        public HookType Type { get; }
        public int HookId { get; }
        public int Channel { get; }

        public SetHookCommand(int soundId, HookType type, int hookId, int channel = 0)
        {
            SoundId = soundId;
            Type = type;
            HookId = hookId;
            Channel = channel;
        }

        public override void Execute(ImuseCommands commands)
        {
            commands.SetHook(SoundId, Type, HookId, Channel);
        }
    }

    /// <summary>
    /// Triggers start sounds
    /// </summary>
    public class QueueItem
    {
        public int SoundId { get; }
        public int MarkerId { get; }

        public IReadOnlyList<QueueCommand> Commands { get; }

        public QueueItem(int soundId, int markerId, List<QueueCommand> commands)
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

        public void Enqueue(QueueItem item)
        {
            items.Enqueue(item);
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
                    cmd.Execute(engine.Commands);
                }
            }
        }

        public void Clear()
        {
            items.Clear();
        }
    }
}
