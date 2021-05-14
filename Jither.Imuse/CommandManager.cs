using Jither.Imuse.Files;
using Jither.Imuse.Helpers;
using Jither.Imuse.Scripting.Runtime;
using Jither.Imuse.Scripting.Types;
using Jither.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace Jither.Imuse
{
    /// <summary>
    /// Indicates that the method should not be exposed to interpreter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NoScriptingAttribute : Attribute
    {

    }

    public class CommandManager
    {
        private static readonly Random rng = new();

        private static readonly Logger logger = LogProvider.Get(nameof(CommandManager));

        private readonly PlayerManager players;
        
        public CommandManager(PlayerManager players)
        {
            this.players = players;
        }

        public void StartSound(int sound)
        {
            players.StartSound(sound);
        }

        public void StopSound(int sound)
        {
            players.StopSound(sound);
        }

        public void ClearLoop(int sound)
        {
            GetPlayer(sound)?.Sequencer.ClearLoop();
        }

        [NoScripting] // We use dedicated commands for each hook type in scripting
        public void SetHook(int sound, HookType type, int hook, int channel)
        {
            GetPlayer(sound)?.HookBlock.SetHook(type, hook, channel);
        }

        public void SetJumpHook(int sound, int hook)
        {
            SetHook(sound, HookType.Jump, hook, 0);
        }

        public void SetTransposeHook(int sound, int hook)
        {
            SetHook(sound, HookType.Transpose, hook, 0);
        }

        public void SetPartTransposeHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartTranspose, hook, channel);
        }

        public void SetPartPgmchHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartProgramChange, hook, channel);
        }

        public void SetPartVolumeHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartVolume, hook, channel);
        }

        public void SetPartEnableHook(int sound, int hook, int channel)
        {
            SetHook(sound, HookType.PartEnable, hook, channel);
        }

        public void JumpTo(int sound, int track, Time time)
        {
            GetPlayer(sound).Sequencer.Jump(track, time.Beat, time.Tick, "command");
        }

        public void PrintLine(string line)
        {
            logger.Info(line);
        }

        public int Random(int min, int max)
        {
            // Unlike C#, random upper is inclusive
            return rng.Next(min, max + 1);
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
