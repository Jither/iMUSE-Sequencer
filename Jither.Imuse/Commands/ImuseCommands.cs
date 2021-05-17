using Jither.Imuse.Files;
using Jither.Imuse.Helpers;
using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Types;
using Jither.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace Jither.Imuse.Commands
{
    public class ImuseCommands
    {
        private readonly PlayerManager players;

        public ImuseCommands(PlayerManager players)
        {
            this.players = players;
        }

        [Enqueueable]
        public void StartSound(int sound)
        {
            players.StartSound(sound);
        }

        [Enqueueable]
        public void StopSound(int sound)
        {
            players.StopSound(sound);
        }

        [Enqueueable]
        public void RestartSound(int sound)
        {
            StopSound(sound);
            StartSound(sound);
        }

        [Enqueueable]
        public void ClearLoop(int sound)
        {
            GetPlayer(sound)?.Sequencer.ClearLoop();
        }

        [NoScripting] // We use dedicated commands for each hook type in scripting
        public void SetHook(int sound, HookType type, int hook, int channel)
        {
            GetPlayer(sound)?.HookBlock.SetHook(type, hook, channel);
        }

        [Enqueueable]
        public void SetJumpHook(int sound, int hook)
        {
            SetHook(sound, HookType.Jump, hook, 0);
        }

        [Enqueueable]
        public void SetTransposeHook(int sound, int hook)
        {
            SetHook(sound, HookType.Transpose, hook, 0);
        }

        [Enqueueable]
        public void SetPartTransposeHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartTranspose, hook, channel);
        }

        [Enqueueable]
        public void SetPartPgmchHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartProgramChange, hook, channel);
        }

        [Enqueueable]
        public void SetPartVolumeHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartVolume, hook, channel);
        }

        [Enqueueable]
        public void SetPartEnableHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartEnable, hook, channel);
        }

        [Enqueueable]
        public void JumpTo(int sound, int track, Time time)
        {
            GetPlayer(sound).Sequencer.Jump(track, time.Beat, time.Tick, "command");
        }

        [NoScripting]
        public InteractivityInfo GetInteractivityInfo(int sound)
        {
            return GetPlayer(sound)?.GetInteractivityInfo();
        }

        [NoScripting]
        public Player GetPlayer(int soundId)
        {
            return players.GetPlayerBySound(soundId);
        }
    }
}
