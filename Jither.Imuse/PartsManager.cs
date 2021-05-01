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
                slots.Add(new Slot(i));
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
            // Find parts needing a slot
            var slotlessParts = new List<Part>();
            foreach (var part in parts)
            {
                if (part.NeedsSlot)
                {
                    slotlessParts.Add(part);
                }
            }
            
            if (slotlessParts.Count == 0)
            {
                return;
            }

            // Sort relevant parts by descending priority:
            slotlessParts.Sort((a, b) => b.PriorityEffective - a.PriorityEffective);

            // Sort slots by descending priority (slots not in use have negative effective priority) 
            // This in order to assign the most picky slots first, if possible
            var slotCandidates = slots.OrderBy(slot => slot.PriorityEffective).ToList();

            int slotIndex = 0;
            foreach (var part in slotlessParts)
            {
                for (int i = slotIndex; i < slotCandidates.Count; i++)
                {
                    var slot = slotCandidates[i];
                    if (slot.PriorityEffective < part.PriorityEffective)
                    {
                        slot.AssignPart(part);
                        driver.UpdateSetup(part);
                        driver.SetVolume(part);
                        driver.SetModWheel(part);
                        driver.SetSustain(part);
                        driver.SetPitchOffset(part);

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
