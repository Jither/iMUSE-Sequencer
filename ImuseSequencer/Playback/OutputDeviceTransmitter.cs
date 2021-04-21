using Jither.Imuse;
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
    public class OutputDeviceTransmitter : ITransmitter, IDisposable
    {
        private static readonly Logger logger = LogProvider.Get(nameof(OutputDeviceTransmitter));
        private readonly OutputDevice output;
        private MidiScheduler<MidiEvent> scheduler;
        private List<MidiEvent> events;

        private bool disposed;

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
            if (scheduler == null)
            {
                scheduler = new MidiScheduler<MidiEvent>(500000, ticksPerQuarterNote);
                scheduler.SliceReached += slice =>
                {
                    for (int i = 0; i < slice.Count; i++)
                    {
                        var message = slice[i].Message;
                        if (message is SetTempoMessage meta)
                        {
                            scheduler.MicrosecondsPerBeat = meta.Tempo;
                        }
                        else
                        {
                            logger.Verbose($"{scheduler.TimeInTicks,10} {message}");
                            output.SendMessage(message);
                        }
                    }
                };
                scheduler.TempoChanged += tempo =>
                {
                    logger.Info($"{scheduler.TimeInTicks,10} tempo = {scheduler.BeatsPerMinute:0.00}");
                };
            }
            else
            {
                if (ticksPerQuarterNote != scheduler.TicksPerQuarterNote)
                {
                    throw new ImuseSequencerException($"Initialized with PPQN (ticks per quarter note) value ({ticksPerQuarterNote}) that differs from the scheduler's existing PPQN ({scheduler.TicksPerQuarterNote})");
                }
            }
        }

        // TODO: Temporary hacky way of sending all events when done
        public void Send()
        {
            scheduler.Schedule(events);

            scheduler.Start();
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
        }
    }
}
