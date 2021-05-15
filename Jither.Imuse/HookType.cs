using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Jither.Imuse
{
    /// <summary>
    /// Enumeration of all hook types in iMUSE.
    /// </summary>
    public enum HookType
    {
        [Display(Name = "jump")]
        Jump,
        [Display(Name = "transpose")]
        Transpose,
        [Display(Name = "part-enable")]
        PartEnable,
        [Display(Name = "part-volume")]
        PartVolume,
        [Display(Name = "part-program-change")]
        PartProgramChange,
        [Display(Name = "part-transpose")]
        PartTranspose
    }
}
