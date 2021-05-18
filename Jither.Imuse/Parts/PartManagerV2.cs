using Jither.Imuse.Drivers;
using Jither.Imuse.Files;
using Jither.Imuse.Messages;
using System.Linq;

namespace Jither.Imuse.Parts
{
    public class PartManagerV2 : PartManager
    {
        public PartManagerV2(Driver driver, ImuseOptions options) : base(driver, options)
        {
            // iMUSE v2 auto-allocation - relevant changes on Part/Player trigger the SlotReassignmentRequred event
            foreach (var part in parts)
            {
                part.SlotReassignmentRequired += AssignFreeSlots;
            }
        }

        public override Part AutoAllocPart(Player player, IPartAllocation alloc)
        {
            logger.Verbose($"Auto-allocating part for channel {alloc.Channel}");
            return AllocPart(player, alloc);
        }

        public override Part ExplicitAllocPart(Player player, IPartAllocation alloc)
        {
            // No explicit allocation in V2+
            return null;
        }

        public override void DeallocAllParts(Player player)
        {
            // Never called in V2+
        }

        public override void DeallocPart(int channel)
        {
            // Never called in V2+
        }

        protected override void AssignSlot(Part part)
        {
            AssignFreeSlots();
        }

        protected override void UnlinkSlotless(Part part)
        {
            // V2 doesn't keep track of slotless parts
        }

        private void AssignFreeSlots()
        {
            while (true)
            {
                // Find the highest priority slotless part
                int highestPartPriority = 0;
                Part highestPart = null;
                foreach (var part in parts)
                {
                    if (!part.NeedsSlot || part.PriorityEffective < highestPartPriority)
                    {
                        continue;
                    }
                    highestPartPriority = part.PriorityEffective;
                    highestPart = part;
                }

                if (highestPart == null)
                {
                    return;
                }

                // Find the lowest priority slot - or an unused one
                int lowestSlotPriority = 255;
                Slot lowestSlot = null;
                Slot selectedSlot = null;
                foreach (var slot in slots)
                {
                    if (!slot.IsInUse)
                    {
                        logger.Verbose($"Assigning unused {slot} to {highestPart} (pri: {highestPart.PriorityEffective})");
                        selectedSlot = slot;
                        break;
                    }
                    if (slot.IsInUse)
                    {
                        if (slot.Part.PriorityEffective <= lowestSlotPriority)
                        {
                            lowestSlotPriority = slot.PriorityEffective;
                            lowestSlot = slot;
                        }
                    }
                }

                // If we didn't find an unused slot, try the one with the lowest priority
                if (selectedSlot == null)
                {
                    if (lowestSlotPriority >= highestPartPriority)
                    {
                        return;
                    }

                    selectedSlot = lowestSlot;

                    logger.Verbose($"Stealing {selectedSlot} from {selectedSlot.Part} (pri: {selectedSlot.PriorityEffective}) for {highestPart} (pri: {highestPart.PriorityEffective})");

                    selectedSlot.Part.StopAllNotes();
                    selectedSlot.AbandonPart();
                }

                if (selectedSlot != null)
                {
                    AssignSlotToPart(highestPart, selectedSlot);
                }
                else
                {
                    logger.Verbose($"No available slot for {highestPart}");
                }
            }
        }
    }
}
