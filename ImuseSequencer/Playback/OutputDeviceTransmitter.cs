using Jither.Imuse;
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
        private readonly OutputDevice output;
        private MidiScheduler<MidiEvent> scheduler;
        private readonly List<MidiEvent> events;

        private bool disposed;

        public Action<long> Player { get; set; }

        public OutputDeviceTransmitter(int deviceId)
        {
            try
            {
                output = new WindowsOutputDevice(deviceId);
            }
            catch (MidiDeviceException ex)
            {
                throw new ImuseSequencerException($"Failed to connect to output: {ex.Message}");
            }

            // TODO: Preparation for interactive mode. Collect events before sending them to MidiScheduler to reduce amount of locks.
            events = new List<MidiEvent>();
        }

        public void Transmit(MidiEvent evt)
        {
            events.Add(evt);
        }

        public void TransmitImmediate(MidiMessage message)
        {
            // TODO: Also send this through scheduler
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
                            // Prepare and send another batch of MIDI messages
                            Send();
                            break;
                        case SetTempoMessage setTempo:
                            scheduler.MicrosecondsPerBeat = setTempo.Tempo;
                            break;
                        case MetaMessage meta:
                            logger.Verbose($"Discarding meta message: {message}");
                            break;
                        default:
                            // All other messages are sent to output
                            logger.Verbose($"{scheduler.TimeInTicks,10} {message}");
                            output.SendMessage(message);
                            break;
                    }
                }
            };
            scheduler.TempoChanged += tempo =>
            {
                logger.Info($"{scheduler.TimeInTicks,10} tempo = {scheduler.BeatsPerMinute:0.00}");
            };
        }

        public void Start()
        {
            Send();
            scheduler.Start();
        }

        private void Send()
        {
            // Get events for next 480 ticks
            Player?.Invoke(480);

            logger.Verbose($"Sending {events.Count} events to scheduler...");
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

            scheduler.Dispose();
            output.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
