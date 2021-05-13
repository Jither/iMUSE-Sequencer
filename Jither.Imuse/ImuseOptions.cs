namespace Jither.Imuse
{
    public class ImuseOptions
    {
        public int JumpLimit { get; set; } = int.MaxValue;
        public int LoopLimit { get; set; } = int.MaxValue;
        public bool MaxSlots { get; set; } = false;
        public bool CleanJumps { get; set; } = false;
    }
}
