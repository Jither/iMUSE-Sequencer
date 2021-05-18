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
        private readonly ImuseQueue queue;

        public ImuseCommands(PlayerManager players, ImuseQueue queue)
        {
            this.players = players;
            this.queue = queue;
        }

        [Enqueueable]
        public void SetMasterVolume(int volume)
        {
            throw new NotImplementedException();
        }

        public int GetMasterVolume()
        {
            // TODO: This is enqueuable - implement return value handling in queue
            throw new NotImplementedException();
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
        public void StopAllSounds()
        {
            players.StopAllSounds();
        }

        [Enqueueable]
        public void RestartSound(int sound)
        {
            StopSound(sound);
            StartSound(sound);
        }

        // TODO: This is enqueuable
        public int GetPlayStatus(int sound)
        {
            // 0 = not playing
            // 1 = playing
            // 2 = pending in queue
            return (int)players.GetStatus(sound);
        }

        // TODO: get_param

        [Enqueueable]
        public void SetPriority(int sound, int priority)
        {
            GetPlayer(sound)?.SetPriority(priority);
        }

        [Enqueueable]
        public void SetVolume(int sound, int volume)
        {
            GetPlayer(sound)?.SetVolume(volume);
        }

        [Enqueueable]
        public void SetPan(int sound, int pan)
        {
            GetPlayer(sound)?.SetPan(pan);
        }

        [Enqueueable]
        public void SetTranspose(int sound, int transpose, bool relative)
        {
            GetPlayer(sound)?.SetTranspose(transpose, relative);
        }

        [Enqueueable]
        public void SetDetune(int sound, int detune)
        {
            GetPlayer(sound)?.SetDetune(detune);
        }

        [Enqueueable]
        public void SetSpeed(int sound, int speed)
        {
            throw new NotImplementedException();
        }

        [Enqueueable]
        public void JumpTo(int sound, int track, Time time)
        {
            GetPlayer(sound).Sequencer.Jump(track, time.Beat, time.Tick, "command");
        }

        // TODO: scan-to?

        [Enqueueable]
        public void EnablePart(int sound, int channel)
        {
            GetPlayer(sound)?.Parts.SetEnabled(channel, enabled: true);
        }

        [Enqueueable]
        public void DisablePart(int sound, int channel)
        {
            GetPlayer(sound)?.Parts.SetEnabled(channel, enabled: false);
        }

        [Enqueueable]
        public void SetPartVolume(int sound, int channel, int volume)
        {
            GetPlayer(sound)?.Parts.SetVolume(channel, volume);
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
        public void FadeVolume(int sound, int volume, int duration)
        {
            throw new NotImplementedException();
        }

        [Enqueueable]
        public void SetLoop(int sound, int count, Time start, Time end)
        {
            GetPlayer(sound)?.Sequencer.SetLoop(count, start.Beat, start.Tick, end.Beat, end.Tick);
        }

        [Enqueueable]
        public void ClearLoop(int sound)
        {
            GetPlayer(sound)?.Sequencer.ClearLoop();
        }

        public void AnticipateSound(int sound, int flag)
        {
            // TODO: AnticipateSound/PrepareSound is a no-op for ROL and GMD - may be used for ADL?
        }

        // TODO: flush-sound-q?

        public void ClearQueue()
        {
            queue.Clear();
        }

        private Player GetPlayer(int soundId)
        {
            return players.GetPlayerBySound(soundId);
        }
    }
}
