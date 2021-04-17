using Jither.Logging;
using Jither.Midi.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public enum SequencerStatus
    {
        Off,
        On
    }

    /// <summary>
    /// Each Player has a dedicated Sequencer handling the sequencing of the sound that's currently assigned to that player.
    /// </summary>
    public class Sequencer
    {
        private static readonly Logger logger = LogProvider.Get(nameof(Sequencer));
        private MidiFile file;

        private int currentTrackIndex; // index of track within file
        private int nextEventIndex; // index of next event within track

        private int loopsRemaining;
        private int loopStartBeat;
        private int loopStartTick;
        private int loopEndBeat;
        private int loopEndTick;

        private int ticksPerQuarterNote;
        private long totalTick;
        private int beat;
        private long tickInBeat;
        private long nextEventTick;
        
        private bool cancelPlayback;

        public SequencerStatus Status { get; private set; }

        public void Start(MidiFile file)
        {
            this.file = file;
            ticksPerQuarterNote = file.TicksPerQuarterNote;
            currentTrackIndex = 0;
            nextEventIndex = 0;

            loopsRemaining = 0;
            loopStartBeat = 1;
            loopStartTick = 0;
            loopEndBeat = 1;
            loopEndTick = 0;

            //SetMicroSecondsPerQuarter(500000);
            //SetSpeed(file.ImuseHeader?.Speed ?? 128);

            totalTick = nextEventTick = GetNextEventTick();

            tickInBeat = totalTick;
            beat = 1;
            UpdateBeats();

            Status = SequencerStatus.On;
        }

        public void Stop()
        {
            Status = SequencerStatus.Off;
            cancelPlayback = true;
        }

        public bool SetLoop(int count, int startBeat, int startTick, int endBeat, int endTick)
        {
            if (startBeat + 1 >= endBeat)
            {
                // Length of the loop is 0 or less - no can do.
                return false;
            }

            if (startBeat < 1)
            {
                startBeat = 1;
            }

            loopStartBeat = startBeat;
            loopStartTick = startTick;
            loopEndBeat = endBeat;
            loopEndTick = endTick;
            loopsRemaining = count;

            return true;
        }

        public void ClearLoop()
        {
            loopsRemaining = 0;
        }

        /// <summary>
        /// Processes events occurring in the next N ticks.
        /// </summary>
        public void Process(int ticks)
        {
            if (Status != SequencerStatus.On)
            {
                return;
            }

            totalTick += ticks;
            tickInBeat += ticks;
            UpdateBeats();

            if (loopsRemaining > 0)
            {
                if (beat >= loopEndBeat && tickInBeat >= loopEndTick)
                {
                    loopsRemaining--;
                    Jump(currentTrackIndex, loopStartBeat, loopStartTick);
                }

                while (totalTick >= nextEventTick)
                {
                    bool end = ProcessEvent();
                    nextEventIndex++;
                    if (end) // TODO: Or cancelled
                    {
                        break;
                    }
                    nextEventTick = GetNextEventTick();
                }
            }
        }

        private bool ProcessEvent()
        {
            // TODO
            return false;
        }

        private bool Jump(int track, int beat, int tickInBeat)
        {
            if (Status == SequencerStatus.Off)
            {
                return false;
            }

            if (track < 0 || track >= this.file.TrackCount)
            {
                logger.Error($"Jump to invalid track {track}...");
                return false;
            }

            if (currentTrackIndex < 0 || currentTrackIndex >= this.file.TrackCount)
            {
                logger.Error($"Jump from invalid track {track}...");
                return false;
            }

            // Search through the chosen track for the destination

            if (beat == 0)
            {
                beat = 1;
            }

            long destTick = (beat - 1) * ticksPerQuarterNote;
            destTick += tickInBeat;

            long nextEventTick;
            int nextEventIndex;
            if (track == currentTrackIndex && destTick >= totalTick)
            {
                // Destination is further ahead in the current track
                nextEventTick = this.nextEventTick;
                nextEventIndex = this.nextEventIndex;
            }
            else
            {
                nextEventIndex = 0;
                nextEventTick = GetNextEventTick(track, nextEventIndex);
            }

            while (nextEventTick < destTick)
            {
                nextEventIndex++; // Yes, mp_jump_midi_msg amounts to this in our implementation
                if (nextEventIndex >= file.Tracks[track].Events.Count)
                {
                    logger.Error($"Jump past track {track} end...");
                    return false;
                }
                nextEventTick = GetNextEventTick(track, nextEventIndex);
            }

            // Now we've found the destination position - transfer state to the sequencer

            this.beat = beat;
            this.tickInBeat = tickInBeat;
            this.totalTick = destTick;
            this.nextEventIndex = nextEventIndex;
            this.nextEventTick = nextEventTick;

            if (this.currentTrackIndex != track)
            {
                this.currentTrackIndex = track;
                // Clear looping - we started a new track
                this.loopsRemaining = 0;
            }

            return true;
        }

        private long GetNextEventTick()
        {
            return this.GetNextEventTick(currentTrackIndex, nextEventIndex);
        }

        private long GetNextEventTick(int trackIndex, int nextEventIndex)
        {
            return file.Tracks[trackIndex].Events[nextEventIndex].AbsoluteTicks;
        }

        private void UpdateBeats()
        {
            while (tickInBeat >= ticksPerQuarterNote)
            {
                beat++;
                tickInBeat -= ticksPerQuarterNote;
            }
        }
    }
}
