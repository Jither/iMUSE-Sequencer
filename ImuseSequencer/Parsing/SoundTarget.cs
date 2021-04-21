using System.Runtime.Serialization;

namespace ImuseSequencer
{
    public enum SoundTarget
    {
        [EnumMember(Value = "Unknown")]
        Unknown,
        [EnumMember(Value = "Adlib")]
        Adlib,
        [EnumMember(Value = "Roland MT-32")]
        Roland,
        [EnumMember(Value = "SoundBlaster")]
        SoundBlaster,
        [EnumMember(Value = "General MIDI")]
        GeneralMidi,
        [EnumMember(Value = "Tandy")]
        Tandy,
        [EnumMember(Value = "Speaker")]
        Speaker
    }
}
