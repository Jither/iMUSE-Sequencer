using Jither.Imuse;
using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Devices;
using Jither.Midi.Devices.Windows;
using Jither.Midi.Messages;
using System;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public class OutputStreamTransmitter : ITransmitter
    {
        private static readonly Logger logger = LogProvider.Get(nameof(OutputStreamTransmitter));
        // TODO: Don't hardcode WindowsDeviceProvider
        private readonly DeviceProvider deviceProvider = new WindowsDeviceProvider();
        private readonly OutputStream stream;
        private readonly int ticksPerBatch;

        private bool disposed;
        private bool done;

        public string OutputName => $"{stream.Name} (streaming)";

        public ImuseEngine Engine { get; set; }

        public OutputStreamTransmitter(string selector, int ticksPerBatch)
        {
            this.ticksPerBatch = ticksPerBatch;
            try
            {
                stream = deviceProvider.GetOutputStream(selector);
                stream.NoOpOccurred += Stream_NoOpOccurred;
            }
            catch (MidiDeviceException ex)
            {
                throw new ImuseSequencerException($"Failed to connect to output: {ex.Message}");
            }
        }

        private void Stream_NoOpOccurred(int obj)
        {
            if (!done)
            {
                Play();
            }
        }

        public void Init(int ticksPerQuarterNote)
        {
            stream.Division = ticksPerQuarterNote;
        }

        public void Transmit(MidiEvent evt)
        {
            if (evt.Message is NoOpMessage)
            {
                stream.WriteNoOp(evt.AbsoluteTicks, 0x69);
                stream.Flush();
            }
            else
            {
                stream.Write(evt);
            }
        }

        private void Play()
        {
            // Get events for next ticks
            long ticksPlayed = Engine.Play(ticksPerBatch);

            // Zero ticks played means the engine is done playing
            if (ticksPlayed == 0)
            {
                done = true;
            }
        }

        public void TransmitImmediate(MidiMessage message)
        {
            // TODO: Might want direct calls to midiOut instead
            stream.Write(new MidiEvent(-1, message));
        }

        public void Start()
        {
            if (Engine == null)
            {
                throw new InvalidOperationException($"{nameof(Engine)} was not set on transmitter.");
            }

            //Task.Run(stream.Run);

            // Engine.Init must be called before starting the stream - it sets PPQN on the stream, which much be done while the stream is stopped.
            Engine.Init();

            stream.Flush();
            stream.Play();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            stream?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
