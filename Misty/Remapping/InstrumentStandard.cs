using System.ComponentModel.DataAnnotations;

namespace Misty.Remapping;

public enum InstrumentStandard
{
    [Display(Name = "Unknown standard")]
    Unknown,
    [Display(Name = "General MIDI")]
    GeneralMidi,
    [Display(Name = "Roland MT-32")]
    RolandMT32,
}
