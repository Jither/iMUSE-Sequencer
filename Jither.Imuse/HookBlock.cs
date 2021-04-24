using Jither.Logging;
using Jither.Utilities;
using System;

namespace Jither.Imuse
{
    /// <summary>
    /// The HookBlock handles hooks (conditional iMUSE midi messages) for a single Player.
    /// </summary>
    public class HookBlock
    {
        private static readonly Logger logger = LogProvider.Get(nameof(HookBlock));
        private readonly Player player;

        public int Jump { get; private set; }
        public int Transpose { get; private set; }
        public int[] PartEnable { get; } = new int[16];
        public int[] PartVolume { get; } = new int[16];
        public int[] PartProgramChange { get; } = new int[16];
        public int[] PartTranspose { get; } = new int[16];

        public HookBlock(Player player)
        {
            this.player = player;
        }

        public void SetHook(HookType type, int value, int channel)
        {
            if (type == HookType.Jump || type == HookType.Transpose)
            {
                logger.Info($"Setting {type.GetFriendlyName()} hook to {value}");
            }
            else
            {
                logger.Info($"Setting {type.GetFriendlyName()} hook for channel {channel} to {value}");
            }
            switch (type)
            {
                case HookType.Jump:
                    Jump = value;
                    break;
                case HookType.Transpose:
                    Transpose = value;
                    break;
                case HookType.PartEnable:
                    SetPartHook(PartEnable, value, channel);
                    break;
                case HookType.PartVolume:
                    SetPartHook(PartVolume, value, channel);
                    break;
                case HookType.PartProgramChange:
                    SetPartHook(PartProgramChange, value, channel);
                    break;
                case HookType.PartTranspose:
                    SetPartHook(PartTranspose, value, channel);
                    break;
            }
        }

        public bool HandleJump(int messageHook, int trackIndex, int beat, int tickInBeat)
        {
            if (messageHook == 0 || messageHook == Jump)
            {
                if (messageHook != 0)
                {
                    Jump = 0;
                }
                logger.Info($"hook {messageHook}: jump to track {trackIndex} @ {beat}.{tickInBeat:000}");
                player.Sequencer.Jump(trackIndex, beat, tickInBeat, $"hook {messageHook}");
                
                return true;
            }
            return false;
        }

        public bool HandleTranspose(int messageHook, int interval, bool relative)
        {
            if (messageHook == 0 || messageHook == Transpose)
            {
                if (messageHook != 0)
                {
                    Transpose = 0;
                }
                logger.Info($"hook {messageHook}: transpose {interval} semitones{(relative ? " (relative)" : "")}");
                player.SetTranspose(interval, relative);

                return true;
            }

            return false;
        }

        public bool HandlePartEnable(int messageHook, int channel, bool enabled)
        {
            if (messageHook == 0 || messageHook == PartEnable[channel])
            {
                if (messageHook != 0)
                {
                    PartEnable[channel] = 0;
                }
                logger.Info($"hook {messageHook}: {(enabled ? "enable" : "disable")} part on channel {channel}");
                player.Parts.SetEnabled(channel, enabled);

                return true;
            }

            return false;
        }

        public bool HandlePartVolume(int messageHook, int channel, int volume)
        {
            if (messageHook == 0 || messageHook == PartVolume[channel])
            {
                if (messageHook != 0)
                {
                    PartVolume[channel] = 0;
                }

                logger.Info($"hook {messageHook}: set volume = {volume} on channel {channel}");
                player.Parts.SetVolume(channel, volume);

                return true;
            }

            return false;
        }

        public bool HandlePartProgramChange(int messageHook, int channel, int program)
        {
            if (messageHook == 0 || messageHook == PartProgramChange[channel])
            {
                if (messageHook != 0)
                {
                    PartProgramChange[channel] = 0;
                }

                logger.Info($"hook {messageHook}: change program = {program} on channel {channel}");
                player.Parts.DoProgramChange(channel, program);

                return true;
            }

            return false;
        }

        public bool HandlePartTranspose(int messageHook, int channel, int interval, bool relative)
        {
            if (messageHook == 0 || messageHook == PartTranspose[channel])
            {
                if (messageHook != 0)
                {
                    PartTranspose[channel] = 0;
                }
                logger.Info($"hook {messageHook}: transpose {interval} semitones{(relative ? " (relative)" : "")} on channel {channel}");
                player.Parts.SetTranspose(channel, interval, relative);

                return true;
            }

            return false;
        }

        public void Clear()
        {
            Jump = 0;
            Transpose = 0;
            for (int i = 0; i < 16; i++)
            {
                PartEnable[i] = 0;
                PartVolume[i] = 0;
                PartProgramChange[i] = 0;
                PartTranspose[i] = 0;
            }
        }

        private void SetPartHook(int[] hooks, int value, int channel)
        {
            if (channel < 16)
            {
                hooks[channel] = value;
            }
            else
            {
                Array.Fill(hooks, value);
            }
        }
    }
}
