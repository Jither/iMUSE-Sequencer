﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jither.Midi.Parsing;
using Jither.Utilities;

namespace Jither.Midi.Messages
{
    public class Meter
    {
        public long StartTicks { get; }
        public int Numerator { get; }
        public int Denominator { get; }
        public int ClocksPerBeat { get; } // 24 = beat every quarter note, 36 = beat every dotted quarter note
        public int BeatsPerMeasure { get; }
        public int TicksPerBeat { get; }
        public int ThirtySecondNotesPerMidiQuarterNote { get; }

        public int StartMeasure { get; internal set; }
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
            
            // TODO: Integer division here may lead to inaccuracy in rare cases
            TicksPerBeat = ticksPerQuarterNote * 24 / clocksPerBeat;

            // Beats per measure = (quarter notes per measure) / (beat length in quarter notes)
            // Quarter notes per measure = 4 * (numerator) / (denominator)
            // Beat length in quarter notes = clocksPerBeat / 24
            // So, beats per measure = (4 * numerator / denominator) / (clocksPerBeat / 24)
            // For e.g. 6/8:
            // Quarter notes per measure    = 4 * 6 / 8  = 3
            // Beat length in quarter notes = 36 / 24    = 1.5 (1 beat = dotted quarter note)
            // Beats per measure            = 3 / 1.5    = 2
            BeatsPerMeasure = 4 * Numerator * 24 / (Denominator * ClocksPerBeat);
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

        public BeatPosition(long absoluteTicks, Meter meter)
        {
            long ticks = absoluteTicks - meter.StartTicks;
            Tick = (int)(ticks % meter.TicksPerBeat);
            ticks -= Tick;

            long beats = ticks / meter.TicksPerBeat;
            Beat = (int)(beats % meter.BeatsPerMeasure);
            beats -= Beat;

            Measure = (int)(beats / meter.BeatsPerMeasure) + meter.StartMeasure;
        }

        public override string ToString()
        {
            return $"{Measure}.{Beat}.{Tick:000}";
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
            ApplyToFile();
        }

        private void ApplyToFile()
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
                }
                else
                {
                    change.StartMeasure = 0;
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
