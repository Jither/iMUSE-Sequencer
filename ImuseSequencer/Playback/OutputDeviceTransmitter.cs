﻿using Jither.Imuse;
using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Devices;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Messages;
using Jither.Midi.Sequencing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public class OutputDeviceTransmitter : ITransmitter
    {
        private static readonly Logger logger = LogProvider.Get(nameof(OutputDeviceTransmitter));
        // TODO: Don't hardcode WindowsDeviceProvider
        private readonly DeviceProvider deviceProvider = new WindowsDeviceProvider();
        private readonly OutputDevice output;
        private MidiScheduler<MidiEvent> scheduler;
        private readonly List<MidiEvent> events;

        private readonly int ticksPerBatch;

        private bool disposed;

        public string OutputName => output.Name;

        public ImuseEngine Engine { get; set; }

        public OutputDeviceTransmitter(string selector, int ticksPerBatch)
        {
            this.ticksPerBatch = ticksPerBatch;
            try
            {
                output = deviceProvider.GetOutputDevice(selector);
            }
            catch (MidiDeviceException ex)
            {
                throw new ImuseSequencerException($"Failed to connect to output: {ex.Message}");
            }

            events = new List<MidiEvent>();
        }

        public void Transmit(MidiEvent evt)
        {
            events.Add(evt);
        }

        public void TransmitImmediate(MidiMessage message)
        {
            output.SendMessage(message);
        }

        public void Init(int ticksPerQuarterNote)
        {
            scheduler = new MidiScheduler<MidiEvent>(500000, ticksPerQuarterNote);
            scheduler.SliceReached += slice =>
            {
                for (int i = 0; i < slice.Count; i++)
                {
                    var message = slice[i].Message;
                    switch (message)
                    {
                        case NoOpMessage:
                            // We don't really care what the signal is.
                            // Prepare and send another batch of MIDI messages
                            Play();
                            break;
                        case SetTempoMessage setTempo:
                            scheduler.MicrosecondsPerBeat = setTempo.Tempo;
                            break;
                        case MetaMessage meta:
                            logger.Verbose($"Discarding meta message: {message}");
                            break;
                        default:
                            // All other messages are sent to output
                            logger.Debug($"{scheduler.TimeInTicks,10} {message}");
                            output.SendMessage(message);
                            break;
                    }
                }
            };
            scheduler.TempoChanged += tempo =>
            {
                logger.Debug($"tempo = {scheduler.BeatsPerMinute:0.00}");
            };
        }

        public void Start()
        {
            if (Engine == null)
            {
                throw new InvalidOperationException($"{nameof(Engine)} was not set on transmitter.");
            }

            Engine.Init();
            Send();
            scheduler.Start();
        }

        private void Play()
        {
            // Get events for next ticks
            long ticksPlayed = Engine.Play(ticksPerBatch);

            // Zero ticks played means the engine is done playing
            if (ticksPlayed == 0)
            {
                return;
            }

            Send();
        }

        private void Send()
        {
            logger.Debug($"Sending {events.Count} events to scheduler...");
            scheduler.Schedule(events);
            events.Clear();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            scheduler?.Dispose();
            output?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
