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
    // TODO: Get Windows MIDI Stream stuff working - right now, this is completely non-functional
    public class OutputStreamTransmitter : ITransmitter
    {
        private static readonly Logger logger = LogProvider.Get(nameof(OutputStreamTransmitter));
        private readonly WindowsOutputStream stream;

        private bool disposed;

        public ImuseEngine Engine { get; set; }

        public OutputStreamTransmitter(int deviceId)
        {
            try
            {
                stream = new WindowsOutputStream(deviceId);
            }
            catch (MidiDeviceException ex)
            {
                throw new ImuseSequencerException($"Failed to connect to output: {ex.Message}");
            }
        }

        public void Init(int ticksPerQuarterNote)
        {
            stream.Division = ticksPerQuarterNote;
        }

        public void Transmit(MidiEvent evt)
        {
            stream.Write(evt);
        }

        public void TransmitImmediate(MidiMessage message)
        {
            stream.Write(new MidiEvent(-1, message));
        }

        public void Start()
        {
            stream.Flush();
            stream.Start();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            stream.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
