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

        public int ExternalAddress { get; }
        public int SlotSetupAddress { get; }
        // TODO: No need to do bytes here - do a bool per note
        public ushort[] NoteTable { get; } = new ushort[8];

        public Slot(int number, Driver driver)
        {
            this.driver = driver;

            Channel = number + 1; // Channels 2-9 (1-8 zero-indexed), percussion = 10
            ExternalAddress = Roland.RealPartBaseAddress + Roland.RealPartSize * number;
            SlotSetupAddress = Roland.ActiveSetupBaseAddress + Roland.ActiveSetupSize * number;
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
