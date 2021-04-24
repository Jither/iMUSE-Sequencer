using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jither.Midi.Files;
using Jither.Utilities;

namespace Jither.Midi.Messages
{
    public class Meter
    {
        /// <summary>
        /// The tick position where this meter starts.
        /// </summary>
        public long StartTicks { get; }

        /// <summary>
        /// Numerator of the time signature. E.g. for 6/8, this is 6.
        /// </summary>
        public int Numerator { get; }

        /// <summary>
        /// Denominator of the time signature. E.g. for 6/8, this is 8.
        /// </summary>
        public int Denominator { get; }

        /// <summary>
        /// Number of MIDI clocks in a beat. The MIDI default is 24, meaning 1 beat = 1 quarter note.
        /// A value of 36 would indicate that 1 beat = 1 dotted quarter note. So:<br />
        /// For 4/4, this is 24<br />
        /// For 6/8, this is 36<br />
        /// For 5/8, behaviour is undefined. The TIme Signature meta message isn't built for complex time signatures.
        /// </summary>
        public int ClocksPerBeat { get; }

        /// <summary>
        /// Number of beats per measure. This is based on <see cref="ClocksPerBeat" />, but usual values:<br />
        /// For 4/4, this is 4.<br />
        /// For 6/8, this is 2.<br />
        /// For 5/8, behaviour is undefined. The Time Signature meta message isn't built for complex time signatures.
        /// </summary>
        public int BeatsPerMeasure { get; }

        /// <summary>
        /// Number of quarter notes per measure.<br />
        /// For 4/4, this is 4.<br />
        /// For 6/8, this is 3.<br />
        /// For 5/8, this is 2.5.
        /// </summary>
        public decimal QuarterNotesPerMeasure { get; }

        /// <summary>
        /// Number of ticks per beat.
        /// </summary>
        public int TicksPerBeat { get; }

        /// <summary>
        /// Number of ticks per quarter note. This is constant within a file (defined by PPQN in the MIDI file).
        /// </summary>
        public int TicksPerQuarterNote { get; }

        /// <summary>
        /// The number of notated 32nd notes in a MIDI quarter note. This is intended for sequencers that might e.g. use 3/4 in the MIDI
        /// while notating as 3/8.
        /// </summary>
        public int ThirtySecondNotesPerMidiQuarterNote { get; }

        /// <summary>
        /// The measure where this meter starts.
        /// </summary>
        public int StartMeasure { get; internal set; }

        /// <summary>
        /// The total beat where this meter starts (i.e. the beat counted from start of MIDI)
        /// </summary>
        public int StartTotalBeat { get; internal set; }

        public Meter Next { get; private set; }
        public Meter Previous { get; private set; }

        public Meter(long absoluteTicks, TimeSignatureMessage message, int ticksPerQuarterNote)
            : this(absoluteTicks, message.Numerator, message.Denominator, message.ClocksPerBeat, message.ThirtySecondNotesPerMidiQuarterNote, ticksPerQuarterNote)
        {
        }

        public Meter(long startTicks, int numerator, int denominator, int clocksPerBeat, int thirtySecondNotesPerMidiQuarterNote, int ticksPerQuarterNote)
        {
            StartTicks = startTicks;
            Numerator = numerator;
            Denominator = denominator;
            ClocksPerBeat = clocksPerBeat;
            ThirtySecondNotesPerMidiQuarterNote = thirtySecondNotesPerMidiQuarterNote;
            
            // TODO: Integer division here might lead to inaccuracy in rare cases
            TicksPerBeat = ticksPerQuarterNote * 24 / clocksPerBeat;
            TicksPerQuarterNote = ticksPerQuarterNote;

            // Beats per measure = (quarter notes per measure) / (beat length in quarter notes)
            // Quarter notes per measure = 4 * (numerator) / (denominator)
            // Beat length in quarter notes = clocksPerBeat / 24
            // So, beats per measure = (4 * numerator / denominator) / (clocksPerBeat / 24)
            // For e.g. 6/8:
            // Quarter notes per measure    = 4 * 6 / 8  = 3
            // Beat length in quarter notes = 36 / 24    = 1.5 (1 beat = dotted quarter note)
            // Beats per measure            = 3 / 1.5    = 2
            BeatsPerMeasure = 4 * Numerator * 24 / (Denominator * ClocksPerBeat);
            QuarterNotesPerMeasure = 4m * Numerator / Denominator;
        }

        public void Link(Meter previous)
        {
            Previous = previous;
            if (previous != null)
            {
                previous.Next = this;
            }
        }

        public override string ToString()
        {
            string meter = $"{Numerator}/{Denominator}";
            return $"{StartMeasure,4} ({StartTicks,10}) {meter,8} - {ClocksPerBeat} MIDI-clocks/beat, {TicksPerBeat} ticks/beat, {BeatsPerMeasure} beats/measure, 32nds/qn = {ThirtySecondNotesPerMidiQuarterNote}.";
        }
    }

    public class BeatPosition
    {
        public int Measure { get; }
        public int Beat { get; }
        public int Tick { get; }

        public int TotalBeat { get; }

        public BeatPosition(long absoluteTicks, Meter meter)
        {
            long ticks = absoluteTicks - meter.StartTicks;
            Tick = (int)(ticks % meter.TicksPerBeat);
            ticks -= Tick;

            long beats = (ticks / meter.TicksPerBeat);
            TotalBeat = (int)beats + meter.StartTotalBeat;
            Beat = (int)(beats % meter.BeatsPerMeasure);
            beats -= Beat;

            Measure = (int)(beats / meter.BeatsPerMeasure + meter.StartMeasure);
        }

        public override string ToString()
        {
            return $"{ToString(collapseMeasuresToBeats: false)} [{ToString(collapseMeasuresToBeats: true),7}]";
        }

        public string ToString(bool collapseMeasuresToBeats)
        {
            return collapseMeasuresToBeats ? $"{TotalBeat + 1}.{Tick:000}" : $"{Measure + 1}.{Beat + 1}.{Tick:000}";
        }
    }

    public class Timeline : IReadOnlyList<Meter>
    {
        private readonly MidiFile file;

        private List<Meter> timeline = new();

        public Timeline(MidiFile file)
        {
            this.file = file;
            Populate();
        }

        public void ApplyBeatPositions()
        {
            foreach (var track in file.Tracks)
            {
                Meter currentMeter = timeline[0];
                long nextChange = currentMeter.Next?.StartTicks ?? long.MaxValue;

                foreach (var evt in track.Events)
                {
                    if (evt.AbsoluteTicks >= nextChange)
                    {
                        currentMeter = currentMeter.Next;
                        nextChange = currentMeter.Next?.StartTicks ?? long.MaxValue;
                    }

                    evt.BeatPosition = new BeatPosition(evt.AbsoluteTicks, currentMeter);
                }
            }
        }

        private void Populate()
        {
            if (file.DivisionType != DivisionType.Ppqn)
            {
                throw new NotSupportedException($"Beat timing is only supported for MIDIs with PPQN time division.");
            }
            int tpq = file.TicksPerQuarterNote;

            // Build timeline of time signature changes by tick
            foreach (var track in file.Tracks)
            {
                foreach (var evt in track.Events)
                {
                    if (evt.Message is TimeSignatureMessage message)
                    {
                        timeline.Add(new Meter(evt.AbsoluteTicks, message, tpq));
                    }
                }
            }
            timeline = timeline.Distinct(change => change.StartTicks).ToList();

            timeline.Sort((a, b) => a.StartTicks.CompareTo(b.StartTicks));
            if (timeline.Count == 0 || timeline[0].StartTicks != 0)
            {
                // Add default at start
                timeline.Insert(0, new Meter(0, 4, 4, 24, 8, tpq));
            }

            Meter previous = null;
            foreach (var change in timeline)
            {
                // Linked list of meter changes:
                change.Link(previous);

                if (previous != null)
                {
                    // Get beat position of this change relative to last change
                    var beatPosition = new BeatPosition(change.StartTicks, previous);
                    // ... and use it to assign a starting measure for this change
                    change.StartMeasure = previous.StartMeasure + beatPosition.Measure;
                    change.StartTotalBeat = previous.StartTotalBeat + beatPosition.TotalBeat;
                }
                else
                {
                    change.StartMeasure = 0;
                    change.StartTotalBeat = 0;
                }
                previous = change;
            }
        }

        public Meter this[int index] => timeline[index];

        public int Count => timeline.Count;

        public IEnumerator<Meter> GetEnumerator() => timeline.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
