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
        private MidiFile file;

        private int currentChunkIndex; // "Chunk" here refers to MTrk - i.e., this is the track we're currently playing
        private int partialTick;
        private int loopStatus;
        private int loopStartBeat;
        private int loopStartTick;
        private int loopEndBeat;
        private int loopEndTick;

        private int beat;
        private long tick;
        private long tickCount;
        private long nextEventTick;
        
        private int index;

        private bool cancelPlayback;

        public SequencerStatus Status { get; private set; }

        public void Start(MidiFile file)
        {
            this.file = file;
            currentChunkIndex = 0;
            partialTick = 0;
            loopStatus = 0;
            loopStartBeat = 1;
            loopStartTick = 0;
            loopEndBeat = 1;
            loopEndTick = 0;

            // TODO:

            /*
            SetMicroSecondsPerQuarter(500000);
            SetSpeed(file.ImuseHeader?.Speed ?? 128);
            tickCount = nextEventTick = file.Tracks[currentChunkIndex].Events[0].AbsoluteTicks;

            index = DiffPointers(); // This should be replaced by Event index
            tick = tickCount;
            beat = 1;

            while (tick >= ticksPerQuarter)
            {
                beat++;
                tick -= ticksPerQuarter;
            }
            */

            Status = SequencerStatus.On;
        }

        public void Stop()
        {
            Status = SequencerStatus.Off;
            cancelPlayback = true;
        }
    }
}
