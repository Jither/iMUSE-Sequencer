using Jither.Midi.Messages;

namespace Jither.Midi.Files
{
    public interface ISysexParser
    {
        /// <summary>
        /// Manufacturer ID that this SysEx parser reads.
        /// </summary>
        int ManufacturerId { get; }

        /// <summary>
        /// Method that should return a (custom) SysexMessage parsed from the given data.<br/>
        /// Data spans from manufacturer ID until closing F7, inclusively.
        /// </summary>
        SysexMessage Parse(byte[] data);
    }
}
