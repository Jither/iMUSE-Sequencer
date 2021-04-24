using System.Runtime.Serialization;

namespace Jither.Imuse
{
    /// <summary>
    /// Enumeration of all hook types in iMUSE.
    /// </summary>
    public enum HookType
    {
        [EnumMember(Value = "jump")]
        Jump,
        [EnumMember(Value = "transpose")]
        Transpose,
        [EnumMember(Value = "part-enable")]
        PartEnable,
        [EnumMember(Value = "part-volume")]
        PartVolume,
        [EnumMember(Value = "part-program-change")]
        PartProgramChange,
        [EnumMember(Value = "part-transpose")]
        PartTranspose
    }
}
