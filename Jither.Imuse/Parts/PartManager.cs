using Jither.Imuse.Drivers;
using Jither.Logging;
using System;
using System.Collections.Generic;

namespace Jither.Imuse.Parts
{
    public abstract class PartManager
    {
        protected static readonly Logger logger = LogProvider.Get(nameof(PartManager));
        protected const int partCount = 32;

        protected readonly Driver driver;
        protected readonly List<Part> parts = new();
        protected readonly List<Slot> slots = new();

        protected PartManager(Driver driver, ImuseOptions options)
        {
            this.driver = driver;

            for (int i = 0; i < partCount; i++)
            {
                var part = new Part(i, driver);
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
                AssignSlot(part);
            }
            else
            {
                part.UnlinkSlot(); // Effectively sets part.Slot to null - percussion doesn't need a slot
            }

            driver.LoadPart(part);

            logger.Verbose($"Allocated {part}");
            return part;
        }

        public abstract Part ExplicitAllocPart(Player player, IPartAllocation alloc);
        public abstract Part AutoAllocPart(Player player, IPartAllocation alloc);

        public PartsCollection GetCollection(Player player)
        {
            return new PartsCollection(this, player, driver);
        }

        protected Part SelectPart(int priority)
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

        public HashSet<SustainedNote> GetSustainNotes()
        {
            var result = new HashSet<SustainedNote>();
            foreach (var slot in slots)
            {
                driver.GetSustainNotes(slot, result);
            }
            return result;
        }

        protected void UnlinkPart(Part part)
        {
            if (!part.IsInUse)
            {
                return;
            }

            logger.Verbose($"Deallocating {part}");

            if (part.Slot != null)
            {
                ReleaseSlot(part.Slot);
            }
            else if (!part.TransposeLocked)
            {
                UnlinkSlotless(part);
            }

            part.Player.UnlinkPart(part);
        }

        protected virtual void ReleaseSlot(Slot slot)
        {
            driver.StopAllNotes(slot);
            slot.AbandonPart();
        }

        protected void AssignSlotToPart(Part part, Slot slot)
        {
            slot.AssignPart(part);
            UnlinkSlotless(part);
            driver.SetVolume(part);
            driver.SetPan(part);
            driver.SetPitchOffset(part);
            driver.SetModWheel(part);
            driver.SetSustain(part);
            driver.SetReverb(part);
            driver.SetChorus(part);
            driver.DoProgramChange(part);
        }

        public virtual void DeallocPart(int channel)
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

        public virtual void DeallocAllParts(Player player)
        {
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                var part = parts[i];
                if (part.Player == player)
                {
                    UnlinkPart(part);
                }
            }
        }

        protected abstract void AssignSlot(Part part);
        protected abstract void UnlinkSlotless(Part part);
    }
}
