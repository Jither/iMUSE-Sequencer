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

    public abstract class ImuseMidiEvent
    {
        private static Logger logger = LogProvider.Get(nameof(ImuseMidiEvent));

        public long AbsoluteTick { get; }
        public int Channel { get; set; }

        protected ImuseMidiEvent(long absoluteTick, int channel)
        {
            AbsoluteTick = absoluteTick;
            Channel = channel;
        }

        public static ImuseMidiEvent Create(long absoluteTick, MidiMessage message)
        {
            return message switch
            {
                NoteOnMessage noteOn => new NoteOnEvent(absoluteTick, noteOn.Channel, noteOn.Key, noteOn.Velocity),
                NoteOffMessage noteOff => new NoteOffEvent(absoluteTick, noteOff.Channel, noteOff.Key),
                ControlChangeMessage controlChange => new ControlChangeEvent(absoluteTick, controlChange.Channel, controlChange.Controller, controlChange.Value),
                ProgramChangeMessage programChange => new ProgramChangeEvent(absoluteTick, programChange.Channel, programChange.Program),
                PitchBendChangeMessage pitchBend => new PitchBendChangeEvent(absoluteTick, pitchBend.Channel, pitchBend.Bender),
                ImuseMessage imuse => new ImuseEvent(absoluteTick, imuse.Channel, imuse),
                SysexMessage sysex => new SysexEvent(absoluteTick, -1, sysex.Data),
                SetTempoMessage tempo => new SetTempoEvent(absoluteTick, -1, tempo.Tempo),
                EndOfTrackMessage => new EndOfTrackEvent(absoluteTick, -1),
                _ => null
            };
        }
    }

    public class NoteOnEvent : ImuseMidiEvent
    {
        public int Key { get; }
        public int Velocity { get; }

        public NoteOnEvent(long absoluteTick, int channel, int key, int velocity) : base(absoluteTick, channel)
        {
            Key = key;
            Velocity = velocity;
        }
    }

    public class NoteOffEvent : ImuseMidiEvent
    {
        public int Key { get; }

        public NoteOffEvent(long absoluteTick, int channel, int key) : base(absoluteTick, channel)
        {
            Key = key;
        }
    }

    public class ControlChangeEvent : ImuseMidiEvent
    {
        public MidiController Controller { get; }
        public int Value { get; }

        public ControlChangeEvent(long absoluteTick, int channel, MidiController controller, int value) : base(absoluteTick, channel)
        {
            Controller = controller;
            Value = value;
        }
    }

    public class ProgramChangeEvent : ImuseMidiEvent
    {
        public int Program { get; }

        public ProgramChangeEvent(long absoluteTick, int channel, int program) : base(absoluteTick, channel)
        {
            Program = program;
        }
    }

    public class PitchBendChangeEvent : ImuseMidiEvent
    {
        public int Bender { get; }

        public PitchBendChangeEvent(long absoluteTick, int channel, int bender) : base(absoluteTick, channel)
        {
            Bender = bender;
        }
    }

    public class SysexEvent : ImuseMidiEvent
    {
        public byte[] Data { get; }

        public SysexEvent(long absoluteTick, int channel, byte[] data) : base(absoluteTick, channel)
        {
            Data = data;
        }
    }

    public class ImuseEvent : ImuseMidiEvent
    {
        public ImuseMessage Message { get; }

        public ImuseEvent(long absoluteTick, int channel, ImuseMessage message) : base(absoluteTick, channel)
        {
            Message = message;
        }
    }

    public class SetTempoEvent : ImuseMidiEvent
    {
        public int Tempo { get; }

        public SetTempoEvent(long absoluteTick, int channel, int tempo) : base(absoluteTick, channel)
        {
            Tempo = tempo;
        }
    }

    public class EndOfTrackEvent : ImuseMidiEvent
    {
        public EndOfTrackEvent(long absoluteTick, int channel) : base(absoluteTick, channel)
        {

        }
    }

    /// <summary>
    /// Each Player has a dedicated Sequencer handling the sequencing of the sound that's currently assigned to that player.
    /// </summary>
    public class Sequencer
    {
        private static readonly Logger logger = LogProvider.Get(nameof(Sequencer));

        private readonly Player player;
        private MidiFile file;

        private int currentTrackIndex; // index of track within file
        private int nextEventIndex; // index of next event within track

        private int loopsRemaining;
        private int loopStartBeat;
        private int loopStartTick;
        private int loopEndBeat;
        private int loopEndTick;

        private int ticksPerQuarterNote;

        private int totalTick; // accumulated tick that the sequencer is at
        private long currentTick; // tick within the current track that the sequencer is at
        private int currentBeat; // beat within the current track that the seqeucner is at
        private long tickInBeat; // tick within the current beat within the current track
        private long nextEventTick; // tick of next event to be processed
        
        private bool cancelPlayback;

        public SequencerStatus Status { get; private set; }

        public Sequencer(Player player)
        {
            this.player = player;
        }

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

            nextEventTick = GetNextEventTick();

            tickInBeat = 0;
            currentBeat = 1;

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

            for (int i = 0; i < ticks; i++)
            {
                if (loopsRemaining > 0)
                {
                    if (currentBeat >= loopEndBeat && tickInBeat >= loopEndTick)
                    {
                        loopsRemaining--;
                        Jump(currentTrackIndex, loopStartBeat, loopStartTick);
                    }
                }

                while (currentTick >= nextEventTick)
                {
                    bool end = ProcessEvent();
                    nextEventIndex++;
                    if (end || cancelPlayback)
                    {
                        break;
                    }
                    nextEventTick = GetNextEventTick();
                }

                totalTick++;
                currentTick++;
                tickInBeat++;
                if (tickInBeat >= ticksPerQuarterNote)
                {
                    currentBeat++;
                    tickInBeat -= ticksPerQuarterNote;
                }
            }
        }

        private bool ProcessEvent()
        {
            bool trackFinished = false;
            var evt = this.file.Tracks[currentTrackIndex].Events[nextEventIndex];
            var message = evt.Message;
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
                
                // Meta
                case EndOfTrackMessage:
                    trackFinished = true;
                    break;
            }

            // Pass message on to player - with the total tick at which it is to occur
            var internalEvent = ImuseMidiEvent.Create(totalTick, message);
            if (internalEvent == null)
            {
                logger.Warning($"Unsupported MIDI event @ {totalTick}: {message}");
            }
            player.HandleEvent(internalEvent);

            return trackFinished;
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
            if (track == currentTrackIndex && destTick >= currentTick)
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

            this.currentBeat = beat;
            this.tickInBeat = tickInBeat;
            this.currentTick = destTick;
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
    }
}
