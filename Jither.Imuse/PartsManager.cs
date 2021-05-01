using Jither.Imuse.Drivers;
using Jither.Imuse.Messages;
using Jither.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jither.Imuse
{
    public class PartsManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(PartsManager));
        private const int partCount = 32;
        private const int slotCount = 8;

        private readonly Driver driver;
        private readonly List<Part> parts = new();
        private readonly List<Slot> slots = new();

        public PartsManager(Driver driver)
        {
            this.driver = driver;

            for (int i = 0; i < partCount; i++)
            {
                var part = new Part(i, driver);
                part.SlotReassignmentRequired += AssignFreeSlots;
                parts.Add(part);
            }

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

        public void AllocPart(Player player, ImuseAllocPart alloc)
        {
            var priority = Math.Clamp(player.Priority + alloc.PriorityOffset, 0, 255);
            var part = SelectPart(priority);
            if (part == null)
            {
                logger.Warning($"No available part for {alloc}");
                return;
            }

            part.Alloc(alloc);

            player.LinkPart(part);

            if (!part.TransposeLocked)
            {
                var slot = SelectSlot(part.PriorityEffective);
                if (slot != null)
                {
                    slot.AssignPart(part);
                }
            }
            else
            {
                part.UnlinkSlot(); // Effectively sets part.Slot to null - percussion doesn't need a slot
            }

            driver.LoadPart(part);
        }

        public void DeallocPart(int channel)
        {
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var part = parts[i];
                if (part.InputChannel == channel)
                {
                    UnlinkPart(part);
                }
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
            return weakestPart;
        }

        private Slot SelectSlot(int priority)
        {
            Slot weakestSlot = null;
            foreach (var slot in slots)
            {
                if (!slot.IsInUse)
                {
                    return slot;
                }

                if (slot.PriorityEffective <= priority)
                {
                    priority = slot.PriorityEffective;
                    weakestSlot = slot;
                }
            }

            if (weakestSlot != null)
            {
                logger.Verbose($"Stealing slot {weakestSlot.Index} from part {weakestSlot.Part.Index}");
                driver.StopAllNotes(weakestSlot);
                weakestSlot.AbandonPart();
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
            logger.Verbose("Reassigning slots...");
            // TODO: Rid this method of LINQ
            // Find parts needing a slot
            var slotlessParts = parts.Where(p => p.NeedsSlot);
            
            if (!slotlessParts.Any())
            {
                return;
            }

            // Sort relevant parts by descending priority:
            slotlessParts = slotlessParts.OrderByDescending(p => p.PriorityEffective);

            // Sort slots by descending priority, but unused slots first.
            // This way, slots will be assigned in this order:
            // * First, unused slots (this isn't entirely optimal: A high priority part may get an unused slot in spite of qualifying for a low priority slot -
            //   while a lower priority part may not get the low priority slot. However, this is what original iMUSE does).
            // * Then, slots whose parts have lower priority than a slotless part.
            var slotCandidates = slots.OrderBy(slot => slot.IsInUse)
                .ThenByDescending(slot => slot.PriorityEffective)
                .ToList();

            int slotIndex = 0;
            foreach (var part in slotlessParts)
            {
                for (int i = slotIndex; i < slotCandidates.Count; i++)
                {
                    var slot = slotCandidates[i];
                    if (slot.PriorityEffective < part.PriorityEffective)
                    {
                        slot.AssignPart(part);
                        
                        driver.SetVolume(part);
                        driver.SetPan(part);
                        driver.SetPitchOffset(part);
                        driver.SetModWheel(part);
                        driver.SetSustain(part);
                        driver.SetReverb(part);
                        driver.SetChorus(part);

                        driver.DoProgramChange(part);
                        
                        // None of the slots we've tested so far will have lower priority than
                        // later parts (sorted by descending priority), so skip them
                        slotIndex = i + 1;
                        break;
                    }
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
