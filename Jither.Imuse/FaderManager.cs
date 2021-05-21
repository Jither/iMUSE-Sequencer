using Jither.Logging;
using System.Collections.Generic;

namespace Jither.Imuse
{
    public enum FaderStatus
    {
        Off,
        On
    }

    public class Fader
    {
        public int Index { get; }
        public FaderStatus Status { get; set; }
        public Player Player { get; set; }
        public int CurrentLevel { get; set; }
        public int FramesDuration { get; set; } // "time"
        public int FramesRemaining { get; set; } // "counter"
        public int Slope { get; set; }
        public int Nudge { get; set; }
        public int SlopeMod { get; set; }
        public int ModOverflowCounter { get; set; }

        public Fader(int index)
        {
            Index = index;
        }
    }

    public class FaderManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(FaderManager));
        private const int faderCount = 8;

        private readonly List<Fader> faders = new();
        private bool fadersEnabled;
        private long usecCount;
        private long ticks = 0;

        public FaderManager()
        {
            for (int i = 0; i < faderCount; i++)
            {
                faders.Add(new Fader(i));
            }

            fadersEnabled = false;
            usecCount = 0;
        }

        public bool FadeVolume(Player player, int volume, int duration)
        {
            StopFade(player);

            if (duration == 0)
            {
                player.SetVolume(volume);
                return true;
            }

            fadersEnabled = true;

            foreach (var fader in faders)
            {
                if (fader.Status == FaderStatus.Off)
                {
                    fader.Status = FaderStatus.On;
                    fader.Player = player;
                    fader.CurrentLevel = player.Volume; // get_param?
                    fader.FramesDuration = duration;
                    fader.FramesRemaining = duration;
                    int height = volume - fader.CurrentLevel;
                    fader.Slope = height / duration;
                    if (height < 0)
                    {
                        height = -height;
                        fader.Nudge = -1;
                    }
                    else
                    {
                        fader.Nudge = 1;
                    }

                    fader.SlopeMod = height % duration; // TODO: Combine and use double/decimal
                    fader.ModOverflowCounter = 0;
                    return true;
                }
            }

            logger.DebugWarning("Unable to allocate fader for player");
            return false;
        }

        private void StopFade(Player player)
        {
            foreach (var fader in faders)
            {
                if (fader.Status == FaderStatus.On && fader.Player == player)
                {
                    fader.Status = FaderStatus.Off;
                }
            }
        }

        public void Tick()
        {
            if (!fadersEnabled)
            {
                return;
            }

            ticks++;
            // TODO: Temporary hardcode: 96 ticks = 1 frame at 10fps (MI2 frame rate), assuming 120 bpm => 120 * 480 ticks per minute / 60 = 960 ticks per second = 960 / 10 = 96 ticks per frame)
            if ((ticks % 96) != 0)
            {
                return;
            }
            // TODO: Proper time-elapsed for faders
            fadersEnabled = false;
            foreach (var fader in faders)
            {
                if (fader.Status == FaderStatus.On)
                {
                    fadersEnabled = true;
                    int level = fader.CurrentLevel + fader.Slope;
                    fader.ModOverflowCounter += fader.SlopeMod;
                    if (fader.ModOverflowCounter >= fader.FramesDuration)
                    {
                        fader.ModOverflowCounter -= fader.FramesDuration;
                        level += fader.Nudge;
                    }
                    if (level != fader.CurrentLevel)
                    {
                        if (level != 0)
                        {
                            fader.CurrentLevel = level;
                            fader.Player.SetVolume(level);
                        }
                        else
                        {
                            fader.Player.Stop();
                            fader.Status = FaderStatus.Off;
                        }
                    }
                }
                fader.FramesRemaining--;
                if (fader.FramesRemaining == 0)
                {
                    fader.Status = FaderStatus.Off;
                }
            }
        }
    }
}

