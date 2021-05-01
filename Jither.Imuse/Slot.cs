using Jither.Imuse.Drivers;
using System.Collections.Generic;

namespace Jither.Imuse
{
    /// <summary>
    /// Slots represent the usable channels on a synth with limited channels (e.g. MT-32 with 8 melodic channels + 1 percussion).
    /// Parts are assigned to a slot based on priority (combined from player/sound priority + part priority offset). Parts without
    /// a slot are not played.
    /// </summary>
    // TODO: Make base class - this is Roland-specific
    public class Slot
    {
        private Part part;

        /// <summary>
        /// The part currently assigned to this slot.
        /// </summary>
        public Part Part => part;

        /// <summary>
        /// Indicates whether this slot currently has a part assigned.
        /// </summary>
        public bool IsInUse => part != null;

        /// <summary>
        /// The channel that this slot will send to.
        /// </summary>
        public int OutputChannel { get; }

        public int PriorityEffective => part?.PriorityEffective ?? -1;

        public int ExternalAddress { get; }
        public int SlotSetupAddress { get; }
        public HashSet<int> NoteTable { get; } = new();

        public int Index { get; }

        public Slot(int index)
        {
            Index = index;
            OutputChannel = index + 1; // Channels 2-9 (1-8 zero-indexed), percussion = 10 (9 zero-indexed)
            ExternalAddress = Roland.RealPartBaseAddress + Roland.RealPartSize * index;
            SlotSetupAddress = Roland.ActiveSetupBaseAddress + Roland.ActiveSetupSize * index;
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
    }
}
