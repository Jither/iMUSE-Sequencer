using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Jither.Imuse
{
    public enum SoundTarget
    {
        [Display(Name = "Unknown", ShortName = "UNK")]
        Unknown,
        [Display(Name = "Adlib", ShortName = "ADL")]
        Adlib,
        [Display(Name = "Roland MT-32", ShortName = "ROL")]
        Roland,
        [Display(Name = "General MIDI", ShortName = "GMD")]
        GeneralMidi,
        [Display(Name = "Speaker", ShortName = "SPK")]
        Speaker
    }
}
