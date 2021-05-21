using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Jither.Imuse
{
    // Numeric values map to original SCUMM defines for e.g. ADLIB-SFX or ROLAND-SFX
    public enum SoundTarget
    {
        [Display(Name = "Unknown", ShortName = "UNK")]
        Unknown = -1,
        [Display(Name = "Speaker", ShortName = "SPK")]
        Speaker = 0,
        // Tandy = 1
        // CMS = 2
        [Display(Name = "Adlib", ShortName = "ADL")]
        Adlib = 3,
        [Display(Name = "Roland MT-32", ShortName = "ROL")]
        Roland = 4,
        // SoundBlaster = 5
        [Display(Name = "General MIDI", ShortName = "GMD")]
        GeneralMidi = 6,
    }
}
