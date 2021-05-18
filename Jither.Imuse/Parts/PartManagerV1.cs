using Jither.Imuse.Drivers;
using System.Collections.Generic;

namespace Jither.Imuse.Parts
{
    public class PartManagerV1 : PartManager
    {
        private readonly List<Part> slotlessParts = new();

        public PartManagerV1(Driver driver, ImuseOptions options) : base(driver, options)
        {
        }

        public override Part AutoAllocPart(Player player, IPartAllocation alloc)
        {
            // No auto allocation in V1
            return null;
        }

        public override Part ExplicitAllocPart(Player player, IPartAllocation alloc)
        {
            return AllocPart(player, alloc);
        }

        protected override void AssignSlot(Part part)
        {
            Slot slot = SelectSlot(part.PriorityEffective);
            if (slot != null)
            {
                AssignSlotToPart(part, slot);
            }
            else
            {
                LinkSlotless(part);
            }
        }

        private void LinkSlotless(Part part)
        {
            slotlessParts.Add(part);
        }

        protected override void UnlinkSlotless(Part part)
        {
            slotlessParts.Remove(part);
        }

        // TODO: Remove this - no longer in use - but only when ensuring AssignFreeSlots is optimal
        private Slot SelectSlot(int priority)
        {
            Slot weakestSlot = null;
            int lowestPriority = priority;
            foreach (var slot in slots)
            {
                if (!slot.IsInUse)
                {
                    return slot;
                }

                if (slot.PriorityEffective <= lowestPriority)
                {
                    lowestPriority = slot.PriorityEffective;
                    weakestSlot = slot;
                }
            }

            if (weakestSlot != null)
            {
                logger.Verbose($"Stealing {weakestSlot} from {weakestSlot.Part} with priority {lowestPriority} - have a part with priority {priority}");
                driver.StopAllNotes(weakestSlot);
                LinkSlotless(weakestSlot.Part);
                weakestSlot.AbandonPart();
            }

            if (weakestSlot == null)
            {
                logger.Verbose($"No available slot for part (priority: {priority})");
            }

            return weakestSlot;
        }

        private Part FindHighestPrioritySlotlessPart()
        {
            int highestPriority = 0;
            Part highestPart = null;
            foreach (var part in slotlessParts)
            {
                if (part.PriorityEffective > highestPriority)
                {
                    highestPriority = part.PriorityEffective;
                    highestPart = part;
                }
            }
            return highestPart;
        }

        protected override void ReleaseSlot(Slot slot)
        {
            base.ReleaseSlot(slot);
            var highestPart = FindHighestPrioritySlotlessPart();
            if (highestPart != null)
            {
                logger.Verbose($"{highestPart} gets {slot} for good behavior.");
                AssignSlotToPart(highestPart, slot);
            }
        }
    }
}
