using Jither.Logging;
using Jither.Midi.Messages;
using Jither.Midi.Files;
using Jither.Imuse.Parts;

namespace Jither.Imuse
{
    /// <summary>
    /// Each Player has a dedicated Sequencer handling the sequencing of the sound file that's currently assigned to that player.
    /// </summary>
    public class Sequencer
    {
        private enum SequencerStatus
        {
            Off,
            On
        }

        private static readonly Logger logger = LogProvider.Get(nameof(Sequencer));

        internal Player Player { get; }
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

        internal long NextEventTick => nextEventTick;
        internal long CurrentTick => currentTick;
        private MidiTrack CurrentTrack => file.Tracks[currentTrackIndex];

        private SequencerStatus status;

        public int Index { get; }

        public Sequencer(int index, Player player, Sustainer sustainer)
        {
            Index = index;
            this.Player = player;
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

            currentTick = 0;

            //SetMicroSecondsPerQuarter(500000);
            //SetSpeed(file.ImuseHeader?.Speed ?? 128);

            nextEventTick = GetNextEventTick();

            tickInBeat = 0;
            currentBeat = 1;

            status = SequencerStatus.On;
        }

        public void Stop()
        {
            status = SequencerStatus.Off;
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
            logger.Info("Clearing loop");
            loopsRemaining = 0;
        }

        /// <summary>
        /// Processes events at the current tick.
        /// </summary>
        /// <returns>
        /// <c>false</c> if EndOfTrack message was reached during processing. Otherwise <c>true</c> (sequencer active).
        /// </returns>
        public bool Tick()
        {
            if (status != SequencerStatus.On)
            {
                return false;
            }

            // Update beat time
            if (tickInBeat >= ticksPerQuarterNote)
            {
                currentBeat++;
                tickInBeat -= ticksPerQuarterNote;
            }

            // Handle loops
            if (loopsRemaining > 0)
            {
                if (currentBeat >= loopEndBeat && tickInBeat >= loopEndTick)
                {
                    loopsRemaining--;
                    logger.Info($"loop: jump to {loopStartBeat}.{loopStartTick:000} (loops remaining: {loopsRemaining})");
                    Jump(currentTrackIndex, loopStartBeat, loopStartTick, "loop");
                }
            }

            // Process events.
            // Bail is set if a jump or stop occurs during processing of events.
            // This helps us ensure that we don't move to the next event when we're not supposed to.
            bail = false;

            bool done = false;

            while (currentTick >= nextEventTick)
            {
                done = ProcessEvent();
                // We don't need to check done - if done is set, bail is too (but not vice versa)
                if (bail)
                {
                    break;
                }
                nextEventIndex++;
                nextEventTick = GetNextEventTick();
            }
            // We don't need to check end - if done is set, bail is too (but not vice versa)
            if (!bail)
            {
                currentTick++;
                tickInBeat++;
            }

            return !done;
        }

        private bool ProcessEvent()
        {
            var evt = CurrentTrack.Events[nextEventIndex];
            var message = evt.Message;

            bool trackEnded = false;
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
                case EndOfTrackMessage:
                    logger.Verbose("End of track reached...");
                    trackEnded = true;
                    break;
            }

            if (!skip)
            {
                // Pass message on to player
                Player.HandleEvent(message);
            }

            return trackEnded;
        }

        public bool Jump(int newTrackIndex, int newBeat, int newTickInBeat, string reason, bool sustain = true)
        {
            if (status == SequencerStatus.Off)
            {
                return false;
            }

            if (newTrackIndex < 0 || newTrackIndex >= file.TrackCount)
            {
                logger.Error($"Jump to invalid track {newTrackIndex}...");
                return false;
            }

            if (currentTrackIndex < 0 || currentTrackIndex >= file.TrackCount)
            {
                logger.Error($"Jump from invalid track {currentTrackIndex}...");
                return false;
            }

            // Search through the chosen track for the destination

            if (newBeat <= 0)
            {
                newBeat = 1;
            }

            long destTick = (newBeat - 1) * ticksPerQuarterNote;
            destTick += newTickInBeat;

            long newNextEventTick;
            int newNextEventIndex;
            if (newTrackIndex == currentTrackIndex && destTick >= currentTick)
            {
                // Destination is further ahead in the current track, so search from the current position
                newNextEventTick = nextEventTick;
                newNextEventIndex = nextEventIndex;
            }
            else
            {
                // Destination is in a different track or further back in the current track,
                // so search from the beginning of the track
                newNextEventIndex = 0;
                newNextEventTick = GetNextEventTick(newTrackIndex, newNextEventIndex);
            }

            // Find the first event after our destination.
            int newTrackEventCount = file.Tracks[newTrackIndex].Events.Count;

            while (newNextEventTick < destTick)
            {
                newNextEventIndex++; // Yes, mp_jump_midi_msg amounts to this in our implementation
                if (newNextEventIndex >= newTrackEventCount)
                {
                    logger.Error($"Jump past track {newTrackIndex} end...");
                    return false;
                }
                newNextEventTick = GetNextEventTick(newTrackIndex, newNextEventIndex);
            }

            // Now we've found the destination position - stop (MIDI controller) sustain:
            // TODO: In iMUSE v3, this also resets modwheel and pitchbend - is that OK for iMUSE v1-2 too?
            Player.StopAllSustains();

            if (sustain)
            {
                // ... handle sustained notes:
                var oldTrackPos = new SequencerPointer(CurrentTrack, nextEventIndex);
                var newTrackPos = new SequencerPointer(file.Tracks[newTrackIndex], newNextEventIndex);
                sustainer.AnalyzeSustain(this, oldTrackPos, newTrackPos, newNextEventTick - destTick);
            }
            else
            {
                // ... or (v3) cut off notes immediately (and reset controllers)
                Player.StopAllNotesForJump();
            }

            // ... and transfer state to the sequencer:
            currentBeat = newBeat;
            tickInBeat = newTickInBeat;
            currentTick = destTick;
            nextEventIndex = newNextEventIndex;
            nextEventTick = newNextEventTick;

            if (currentTrackIndex != newTrackIndex)
            {
                currentTrackIndex = newTrackIndex;
                // Clear looping - we started a new track
                loopsRemaining = 0;
            }

            // Emit jump meta marker
            Player.HandleEvent(new MarkerMessage($"jump ({reason})"));

            // Make sequencer bail from this tick - we've jumped, so it shouldn't update e.g. nextEventIndex
            bail = true;

            return true;
        }

        private long GetNextEventTick()
        {
            return CurrentTrack.Events[nextEventIndex].AbsoluteTicks;
        }

        private long GetNextEventTick(int trackIndex, int nextEventIndex)
        {
            return file.Tracks[trackIndex].Events[nextEventIndex].AbsoluteTicks;
        }
    }
}
