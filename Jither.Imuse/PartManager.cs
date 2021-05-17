using Jither.Imuse.Drivers;
using Jither.Imuse.Messages;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse
{
    public class PartManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(PartManager));
        private const int partCount = 32;

        private readonly Driver driver;
        private readonly List<Part> parts = new();
        private readonly List<Slot> slots = new();

        public PartManager(Driver driver, ImuseOptions options)
        {
            this.driver = driver;

            for (int i = 0; i < partCount; i++)
            {
                var part = new Part(i, driver);
                part.SlotReassignmentRequired += AssignFreeSlots;
                parts.Add(part);
            }

            int slotCount = options.MaxSlots ? 15 : driver.DefaultSlotCount;

            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new Slot(i, driver.GetChannelForSlot(i)));
            }
        }

        public void Terminate()
        {
            foreach (var part in parts)
            {
                UnlinkPart(part);
            }
        }

        public Part AllocPart(Player player, IPartAllocation alloc)
        {
            var priority = Math.Clamp(player.Priority + alloc.PriorityOffset, 0, 255);
            var part = SelectPart(priority);
            if (part == null)
            {
                logger.Warning($"No available part for {alloc}");
                return null;
            }

            part.Alloc(alloc);

            player.LinkPart(part);

            if (!part.TransposeLocked)
            {
                AssignFreeSlots();
            }
            else
            {
                part.UnlinkSlot(); // Effectively sets part.Slot to null - percussion doesn't need a slot
            }

            driver.LoadPart(part);

            logger.Verbose($"Allocated {part}");
            return part;
        }

        public void DeallocPart(int channel)
        {
            bool deallocated = false;
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var part = parts[i];
                if (part.InputChannel == channel)
                {
                    UnlinkPart(part);
                    deallocated = true;
                }
            }
            if (deallocated)
            {
                this.AssignFreeSlots();
            }
        }

        public void DeallocAllParts()
        {
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var part = parts[i];
                UnlinkPart(part);
            }
            this.AssignFreeSlots();
        }

        public PartsCollection GetCollection(Player player)
        {
            return new PartsCollection(this, player, driver);
        }

        public HashSet<SustainedNote> GetSustainNotes()
        {
            var result = new HashSet<SustainedNote>();
            foreach (var slot in slots)
            {
                driver.GetSustainNotes(slot, result);
            }
            return result;
        }

        private Part SelectPart(int priority)
        {
            Part weakestPart = null;
            foreach (var part in parts)
            {
                if (!part.IsInUse)
                {
                    return part;
                }

                if (part.PriorityEffective <= priority)
                {
                    priority = part.PriorityEffective;
                    weakestPart = part;
                }
            }

            if (weakestPart != null)
            {
                UnlinkPart(weakestPart);
            }
            logger.Info($"Stealing part {weakestPart}");
            return weakestPart;
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
                weakestSlot.AbandonPart();
            }

            if (weakestSlot == null)
            {
                logger.Verbose($"No slot for part. Priority: {priority}");
            }

            return weakestSlot;
        }

        private void UnlinkPart(Part part)
        {
            if (!part.IsInUse)
            {
                return;
            }

            if (part.Slot != null)
            {
                ReleaseSlot(part.Slot);
            }
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
                    selectedSlot.AssignPart(highestPart);
                    driver.SetVolume(highestPart);
                    driver.SetPan(highestPart);
                    driver.SetPitchOffset(highestPart);
                    driver.SetModWheel(highestPart);
                    driver.SetSustain(highestPart);
                    driver.SetReverb(highestPart);
                    driver.SetChorus(highestPart);
                    driver.DoProgramChange(highestPart);
                }
                else
                {
                    logger.Verbose($"No available slot for {highestPart}");
                }
            }
        }

        private void ReleaseSlot(Slot slot)
        {
            driver.StopAllNotes(slot);
            slot.AbandonPart();

            AssignFreeSlots();
        }
    }
}
