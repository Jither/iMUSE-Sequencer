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
        public int TicksDuration { get; set; } // "time"
        public int TicksRemaining { get; set; } // "counter"
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

        public FaderManager()
        {
            for (int i = 0; i < faderCount; i++)
            {
                faders.Add(new Fader(i));
            }

            fadersEnabled = false;
            usecCount = 0;
        }

        public bool FadeVolume(Player player, int volume, int ticksDuration)
        {
            StopFade(player);

            if (ticksDuration == 0)
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
                    fader.TicksDuration = ticksDuration;
                    fader.TicksRemaining = ticksDuration;
                    int height = volume - fader.CurrentLevel;
                    fader.Slope = height / ticksDuration;
                    if (height < 0)
                    {
                        height = -height;
                        fader.Nudge = -1;
                    }
                    else
                    {
                        fader.Nudge = 1;
                    }

                    fader.SlopeMod = height % ticksDuration; // TODO: Combine and use double/decimal
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

            // TODO: Proper time-elapsed for faders
            //usecCount += USEC_PER_INT; 
            //while (usecCount >= USEC_PER_60TH)
            {
                //usecCount -= USEC_PER_60TH;
                fadersEnabled = false;
                foreach (var fader in faders)
                {
                    if (fader.Status == FaderStatus.On)
                    {
                        fadersEnabled = true;
                        int level = fader.CurrentLevel + fader.Slope;
                        fader.ModOverflowCounter += fader.SlopeMod;
                        if (fader.ModOverflowCounter >= fader.TicksDuration)
                        {
                            fader.ModOverflowCounter -= fader.TicksDuration;
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
                    fader.TicksRemaining--;
                    if (fader.TicksRemaining == 0)
                    {
                        fader.Status = FaderStatus.Off;
                    }
                }
            }
        }
    }
}
