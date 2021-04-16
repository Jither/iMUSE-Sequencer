﻿using ImuseSequencer.Drivers;
using Jither.Logging;
using Jither.Midi.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImuseSequencer.Playback
{
    public class PartsManager
    {
        private static readonly Logger logger = LogProvider.Get(nameof(PartsManager));
        private const int partCount = 32;
        private const int slotCount = 8;

        private readonly Driver driver;
        private readonly List<Part> parts = new();
        private readonly List<Slot> slots = new();

        // Probably overkill to use a LinkedList or sorted list for this, so we just use a generic List
        private readonly List<Part> slotlessParts = new();

        public PartsManager(Driver driver)
        {
            this.driver = driver;

            for (int i = 0; i < partCount; i++)
            {
                parts.Add(new Part(i, driver));
            }

            for (int i = 0; i < slotCount; i++)
            {
                slots.Add(new Slot(i, driver));
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
            LinkPart(player, part);

            part.Alloc(alloc);

            if (!part.TransposeLocked)
            {
                var slot = SelectSlot(part.PriorityEffective);
                if (slot != null)
                {
                    part.LinkSlot(slot);
                }
                else
                {
                    LinkSlotless(part);
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
                if (part.Channel == channel)
                {
                    UnlinkPart(part);
                }
            }
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
                driver.StopAllNotes(weakestSlot);
                LinkSlotless(weakestSlot.Part);
                weakestSlot.AbandonPart();
            }

            return weakestSlot;
        }

        private void LinkPart(Player player, Part part)
        {
            player.LinkPart(part);
            part.LinkPlayer(player);
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
            else if (!part.TransposeLocked)
            {
                UnlinkSlotless(part);
            }
        }

        private Part FindHighestPrioritySlotlessPart()
        {
            Part result = null;
            int highestPriority = 0;
            foreach (var part in slotlessParts)
            {
               if (part.PriorityEffective > highestPriority)
                {
                    highestPriority = part.PriorityEffective;
                    result = part;
                }
            }
            return result;
        }

        private void ReleaseSlot(Slot slot)
        {
            slot.AbandonPart();

            var part = FindHighestPrioritySlotlessPart();
            if (part != null)
            {
                slot.AssignPart(part);
                UnlinkSlotless(part);
                driver.UpdateSetup(part);
                driver.SetVolume(part);
                driver.SetPan(part);
                driver.SetModWheel(part);
                driver.SetSustain(part);
                driver.SetPitchOffset(part);
            }
        }

        private void LinkSlotless(Part part)
        {
            slotlessParts.Add(part);
        }

        private void UnlinkSlotless(Part part)
        {
            slotlessParts.Remove(part);
        }
    }
}