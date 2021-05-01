namespace Jither.Imuse
{
    // Used by auto allocation (iMUSE v2)
    public class DefaultPartAllocation : IPartAllocation
    {
        public int Channel { get; }

        public bool Enabled => true;
        public int PriorityOffset => 0;
        public int Volume => 127;
        public int Pan => 0;
        public int Transpose => 0;
        // iMUSE v2 sets this to true initially, changing it to false on program change etc.
        public bool TransposeLocked => true;
        public int Detune => 0;
        public int PitchBendRange => 2;
        public int Reverb { get; } // 1 for ROL, 64 for GMID
        public int Program => -1;
    }
}
