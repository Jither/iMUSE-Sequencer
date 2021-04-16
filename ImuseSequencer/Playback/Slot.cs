using ImuseSequencer.Drivers;

namespace ImuseSequencer.Playback
{
    // TODO: Make base class - this is Roland-specific
    public class Slot
    {
        private Part part;
        private readonly Driver driver;

        public Part Part => part;
        public bool IsInUse => part != null;
        public int Channel { get; }
        public int PriorityEffective => part?.PriorityEffective ?? 0;

        private readonly int externalAddress;
        private readonly int activeSetupAddress;
        private readonly int[] noteTable = new int[8];

        public Slot(int number, Driver driver)
        {
            this.driver = driver;

            Channel = number + 1; // Channels 2-9 (1-8 zero-indexed), percussion = 10
            externalAddress = Roland.RealPartBaseAddress + Roland.RealPartSize * number;
            activeSetupAddress = Roland.ActiveSetupBaseAddress + Roland.ActiveSetupSize * number;
        }

        public void AssignPart(Part part)
        {
            part.LinkSlot(this);
            this.part = part;
        }

        public void AbandonPart()
        {
            if (part == null)
            {
                return;
            }
            part.UnlinkSlot();
            part = null;
        }

        public void Release()
        {

        }
    }
}
