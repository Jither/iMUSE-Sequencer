using Jither.Imuse;
using Jither.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public class OutputHelpers
    {
        public static readonly OutputHelpers Instance = new();

        protected OutputHelpers()
        {

        }

        public string DetermineDeviceSelector(string fromOptions, SoundTarget target)
        {
            if (fromOptions != null)
            {
                return fromOptions;
            }

            if (Settings.Default.Devices.TryGetValue(target, out string selector))
            {
                return selector;
            }

            throw new ImuseSequencerException($"Could not determine output device for target: {target.GetDisplayName()}. Please specify it on the command-line, or set up preferred devices by target in settings.json (using target ID: {target})");
        }

        public ITransmitter CreateTransmitter(string selector, int latency)
        {
            // TODO: Temporary debugging measure - s:<device-id> selects stream transmitter
            var selectorParts = selector.Split(':');
            string deviceSelector;
            string transmitterType = null;
            if (selectorParts.Length == 2)
            {
                transmitterType = selectorParts[0];
                deviceSelector = selectorParts[1];
            }
            else
            {
                deviceSelector = selectorParts[0];
            }

            return transmitterType switch
            {
                "s" => new OutputStreamTransmitter(deviceSelector, latency),
                _ => new OutputDeviceTransmitter(deviceSelector, latency),
            };
        }

    }
}
