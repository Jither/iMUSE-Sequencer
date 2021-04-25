using Jither.Midi.Messages;
using Jither.Midi.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jither.Imuse.Messages;

namespace Jither.Imuse
{
    /// <summary>
    /// "Transmitter" for writing iMUSE output to Standard MIDI file.
    /// </summary>
    public class MidiFileWriterTransmitter : ITransmitter
    {
        private int ticksPerQuarterNote;
        private long currentTick = 0;

        private readonly List<MidiEvent> events = new();

        public ImuseEngine Engine { get; set; }

        public MidiFileWriterTransmitter()
        {
        }

        public void Init(int ticksPerQuarterNote)
        {
            this.ticksPerQuarterNote = ticksPerQuarterNote;
        }

        public void Start()
        {
            if (Engine == null)
            {
                throw new InvalidOperationException($"{nameof(Engine)} was not set on transmitter.");
            }
            Engine.Play(-1); // Ask the engine to just send everything at once.
        }

        public void Transmit(MidiEvent evt)
        {
            if (evt.Message is NoOpMessage)
            {
                return;
            }
            currentTick = evt.AbsoluteTicks;
            events.Add(evt);
        }

        public void TransmitImmediate(MidiMessage message)
        {
            // Add at same time as previous event
            var evt = new MidiEvent(currentTick, message);
            events.Add(evt);
        }

        public void Write(string path, int format = 1)
        {
            var file = new MidiFile(format, DivisionType.Ppqn, ticksPerQuarterNote);

            var tracks = new List<List<MidiEvent>>();
            if (format == 1 || format == 2)
            {
                foreach (var evt in events)
                {
                    int trackIndex = 0;
                    var message = evt.Message;
                    if (message is ChannelMessage channelMessage)
                    {
                        trackIndex = channelMessage.Channel + 1;
                    }
                    while (trackIndex >= tracks.Count)
                    {
                        tracks.Add(new List<MidiEvent>());
                    }
                    var track = tracks[trackIndex];
                    track.Add(evt);
                }
            }
            else
            {
                tracks.Add(events);
            }

            foreach (var track in tracks)
            {
                // Add EndOfTrack meta message if missing
                MidiEvent lastEvent = track.Count > 0 ? track[^1] : null;
                long endTime = lastEvent?.AbsoluteTicks ?? 0;
                if (lastEvent?.Message is not EndOfTrackMessage)
                {
                    track.Add(new MidiEvent(endTime, new EndOfTrackMessage()));
                }
                file.AddTrack(track);
            }

            file.Save(path);
        }

        public void Dispose()
        {
            // Do nothing
            GC.SuppressFinalize(this);
        }
    }
}
