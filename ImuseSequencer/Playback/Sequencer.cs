using Jither.Logging;
using Jither.Midi.Messages;
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

        private readonly Player player;
        private readonly Sustainer sustainer;
        private MidiFile file;

        private int currentTrackIndex; // index of track within file
        private int nextEventIndex; // index of next event within track

        private int loopsRemaining;
        private int loopStartBeat;
        private int loopStartTick;
        private int loopEndBeat;
        private int loopEndTick;

        private int ticksPerQuarterNote;

        private long currentTick; // tick within the current track that the sequencer is at
        private int currentBeat; // beat within the current track that the seqeucner is at
        private long tickInBeat; // tick within the current beat within the current track
        private long nextEventTick; // tick of next event to be processed
        
        private bool bail;

        internal PartsCollection Parts => player.Parts;
        internal long NextEventTick => nextEventTick;
        internal long CurrentTick => currentTick;

        public SequencerStatus Status { get; private set; }

        public Sequencer(Player player, Sustainer sustainer)
        {
            this.player = player;
            this.sustainer = sustainer;
        }

        public void Start(MidiFile file)
        {
            this.file = file;

            bail = false;

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

            nextEventTick = GetNextEventTick();

            tickInBeat = 0;
            currentBeat = 1;

            Status = SequencerStatus.On;
        }

        public void Stop()
        {
            Status = SequencerStatus.Off;
            bail = true;
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
        /// Processes events at the current tick.
        /// </summary>
        public bool Tick()
        {
            if (Status != SequencerStatus.On)
            {
                return true;
            }

            // Send note-off for sustained notes (if any)
            sustainer.Tick();

            if (tickInBeat >= ticksPerQuarterNote)
            {
                currentBeat++;
                tickInBeat -= ticksPerQuarterNote;
            }

            if (loopsRemaining > 0)
            {
                if (currentBeat >= loopEndBeat && tickInBeat >= loopEndTick)
                {
                    loopsRemaining--;
                    Jump(currentTrackIndex, loopStartBeat, loopStartTick);
                }
            }

            bail = false;
            bool end = false;
            while (currentTick >= nextEventTick)
            {
                end = ProcessEvent();
                if (end || bail)
                {
                    break;
                }
                nextEventIndex++;
                nextEventTick = GetNextEventTick();
            }
            if (!bail)
            {
                currentTick++;
                tickInBeat++;
            }

            return end;
        }

        private bool ProcessEvent()
        {
            bool trackFinished = false;
            var evt = this.file.Tracks[currentTrackIndex].Events[nextEventIndex];
            var message = evt.Message;
            
            bool skip = false;
            switch (message)
            {
                // Channel
                case NoteOnMessage noteOn:
                    // Special case: iMUSE also allows velocity 0 as "note-off"
                    if (noteOn.Velocity == 0)
                    {
                        message = new NoteOffMessage(noteOn.Channel, noteOn.Key, noteOn.Velocity);
                    }
                    break;
                case PolyPressureMessage:
                    skip = true;
                    logger.DebugWarning("Hey! iMUSE would like you to quit the poly pressure...");
                    break;
                case ChannelPressureMessage:
                    skip = true;
                    logger.DebugWarning("Hey! iMUSE would like you to quit the aftertouch...");
                    break;
                // Meta
                case EndOfTrackMessage:
                    trackFinished = true;
                    break;
            }

            if (!skip)
            {
                // Pass message on to player
                player.HandleEvent(message);
            }

            return trackFinished;
        }

        public bool Jump(int trackIndex, int beat, int tickInBeat)
        {
            if (Status == SequencerStatus.Off)
            {
                return false;
            }

            if (trackIndex < 0 || trackIndex >= this.file.TrackCount)
            {
                logger.Error($"Jump to invalid track {trackIndex}...");
                return false;
            }

            if (currentTrackIndex < 0 || currentTrackIndex >= this.file.TrackCount)
            {
                logger.Error($"Jump from invalid track {trackIndex}...");
                return false;
            }

            // Search through the chosen track for the destination

            if (beat == 0)
            {
                beat = 1;
            }

            logger.Debug($"Jumping to track {trackIndex}, beat {beat}, tick {tickInBeat}");

            long destTick = (beat - 1) * ticksPerQuarterNote;
            destTick += tickInBeat;

            long nextEventTick;
            int nextEventIndex;
            if (trackIndex == currentTrackIndex && destTick >= currentTick)
            {
                // Destination is further ahead in the current track
                nextEventTick = this.nextEventTick;
                nextEventIndex = this.nextEventIndex;
            }
            else
            {
                nextEventIndex = 0;
                nextEventTick = GetNextEventTick(trackIndex, nextEventIndex);
            }

            while (nextEventTick < destTick)
            {
                nextEventIndex++; // Yes, mp_jump_midi_msg amounts to this in our implementation
                if (nextEventIndex >= file.Tracks[trackIndex].Events.Count)
                {
                    logger.Error($"Jump past track {trackIndex} end...");
                    return false;
                }
                nextEventTick = GetNextEventTick(trackIndex, nextEventIndex);
            }

            // Now we've found the destination position - stop (MIDI controller) sustain, and handle sustained notes:
            player.StopAllSustains();
            sustainer.AnalyzeSustain(this, file.Tracks[currentTrackIndex], this.nextEventIndex, file.Tracks[trackIndex], nextEventIndex, nextEventTick - destTick);

            // ... and transfer state to the sequencer
            this.currentBeat = beat;
            this.tickInBeat = tickInBeat;
            this.currentTick = destTick;
            this.nextEventIndex = nextEventIndex;
            this.nextEventTick = nextEventTick;

            if (this.currentTrackIndex != trackIndex)
            {
                this.currentTrackIndex = trackIndex;
                // Clear looping - we started a new track
                this.loopsRemaining = 0;
            }

            // Make sequencer bail from this tick - we've jumped, so it shouldn't update e.g. nextEventIndex
            bail = true;

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
    }
}
