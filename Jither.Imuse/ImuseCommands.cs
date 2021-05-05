using Jither.Imuse.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Imuse
{
    public class ImuseCommands
    {
        private readonly PlayerManager players;
        
        public ImuseCommands(PlayerManager players)
        {
            this.players = players;
        }

        public void StartSound(int soundId)
        {
            players.StartSound(soundId);
        }

        public void StopSound(int soundId)
        {
            players.StopSound(soundId);
        }

        public void ClearLoop(int soundId)
        {
            GetPlayer(soundId)?.Sequencer.ClearLoop();
        }

        public void SetHook(int soundId, HookType type, int hook, int channel)
        {
            GetPlayer(soundId)?.HookBlock.SetHook(type, hook, channel);
        }

        public void SetJumpHook(int soundId, int hook)
        {
            SetHook(soundId, HookType.Jump, hook, 0);
        }

        public InteractivityInfo GetInteractivityInfo(int soundId)
        {
            return GetPlayer(soundId)?.GetInteractivityInfo();
        }

        public Player GetPlayer(int soundId)
        {
            return players.GetPlayerBySound(soundId);
        }
    }
}
