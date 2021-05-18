namespace Jither.Imuse.Parts
{
    public interface IPartAllocation
    {
        int Channel { get; }
        bool Enabled { get; }
        int PriorityOffset { get; }
        int Volume { get; }
        int Pan { get; }
        int Transpose { get; }
        bool TransposeLocked { get; }
        int Detune { get; }
        int PitchBendRange { get; }
        int Reverb { get; }
        int Program { get; }
    }
}
