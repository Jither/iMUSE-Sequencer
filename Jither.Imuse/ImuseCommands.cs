﻿using Jither.Imuse.Files;
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

        public void ClearLoop(int soundId)
        {
            GetPlayer(soundId)?.Sequencer.ClearLoop();
        }

        public void SetHook(int soundId, Hook type, int hook, int channel)
        {
            GetPlayer(soundId)?.HookBlock.SetHook(type, hook, channel);
        }

        public void SetJumpHook(int soundId, int hook)
        {
            SetHook(soundId, Hook.Jump, hook, 0);
        }

        public InteractivityInfo GetInteractivityInfo(int soundId)
        {
            return GetPlayer(soundId)?.GetInteractivityInfo();
        }

        private Player GetPlayer(int soundId)
        {
            return players.GetPlayerBySound(soundId);
        }
    }
}