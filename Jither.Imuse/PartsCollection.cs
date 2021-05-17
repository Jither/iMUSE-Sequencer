using Jither.Imuse.Drivers;
using Jither.Imuse.Messages;
using Jither.Logging;
using Jither.Midi.Messages;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Jither.Imuse
{

    public class PartsCollection : IReadOnlyList<Part>
    {
        private static readonly Logger logger = LogProvider.Get(nameof(PartsCollection));

        private readonly Player player;
        private readonly PartManager manager;
        private readonly Driver driver;
        private readonly List<Part> parts = new();
        private readonly Dictionary<int, Part> partsByChannel = new();

        public int Count => parts.Count;

        public Part this[int index] => parts[index];

        public PartsCollection(PartManager manager, Player player, Driver driver)
        {
            this.player = player;
            this.manager = manager;
            this.driver = driver;
        }

        public void HandleChannelMessage(ChannelMessage message)
        {
            GetByChannel(message.Channel)?.HandleChannelMessage(message);
        }

        public void HandleImuseMessage(ImuseMessage message)
        {
            GetByChannel(message.Channel)?.HandleImuseMessage(message);
        }

        public void StopAllNotes()
        {
            foreach (var part in parts)
            {
                part.StopAllNotes();
            }
        }

        public void StopAllSustains()
        {
            foreach (var part in parts)
            {
                part.StopAllSustains();
            }
        }

        public void StopAllNotesForJump()
        {
            foreach (var part in parts)
            {
                part.StopAllNotesForJump();
            }
        }

        public void GetSustainNotes(HashSet<SustainedNote> notes)
        {
            foreach (var part in parts)
            {
                part.GetSustainNotes(notes);
            }
        }

        /// <summary>
        /// Updates the priority offset of a single channel.
        /// </summary>
        public void SetPriority(int channel, int priority)
        {
            var part = GetByChannel(channel);
            if (part != null)
            {
                part.PriorityOffset = priority;
            }
        }

        /// <summary>
        /// Updates the priority offset of OMNI. In this implementation, does nothing. Normally, OMNI
        /// would indicate to update the effective value of every channel to reflect a new priority on
        /// the player. But effective priority is always current in this implementation.
        /// </summary>
        public void SetPriority()
        {
        }

        /// <summary>
        /// Updates the volume of OMNI, and signals the new volume to the driver.
        /// </summary>
        public void SetVolume()
        {
            foreach (var part in parts)
            {
                // Property evaluation automatically reflects new Player value, so just signal driver
                driver.SetVolume(part);
            }
        }

        public void SetVolume(int channel, int volume)
        {
            var part = GetByChannel(channel);
            if (part != null)
            {
                part.Volume = volume;
            }
        }

        /// <summary>
        /// Updates the pan of OMNI, and signals the new pan to the driver.
        /// </summary>
        public void SetPan()
        {
            foreach (var part in parts)
            {
                // Property evaluation automatically reflects new Player value, so just signal driver
                driver.SetPan(part);
            }
        }

        /// <summary>
        /// Updates detune of OMNI, and signals the new detune to the driver.
        /// </summary>
        public void SetDetune()
        {
            foreach (var part in parts)
            {
                // Transpose, detune and pitch bend are combined for MT-32
                driver.SetPitchOffset(part);
            }
        }

        /// <summary>
        /// Updates transposition of a single channel, and signals the new transposition to the driver.
        /// </summary>
        public void SetTranspose(int channel, int interval, bool relative)
        {
            if (interval < -24 || interval > 24)
            {
                return;
            }

            var part = GetByChannel(channel);
            if (part != null && !part.TransposeLocked)
            {
                if (relative)
                {
                    part.Transpose = Math.Clamp(interval + part.Transpose, -7, 7);
                }
                else
                {
                    part.Transpose = interval;
                }
            }
        }

        /// <summary>
        /// Updates transposition of OMNI, and signals the new transposition to the driver.
        /// </summary>
        public void SetTranspose()
        {
            foreach (var part in parts)
            {
                // Percussion parts don't transpose
                if (!part.TransposeLocked)
                {
                    // Transpose, detune and pitch bend are combined for MT-32
                    driver.SetPitchOffset(part);
                }
            }
        }

        public void SetEnabled(int channel, bool enabled)
        {
            var part = GetByChannel(channel);
            if (part != null)
            {
                part.Enabled = enabled;
            }
        }

        public void SetupParam(int channel, int setupNumber, int value)
        {
            GetByChannel(channel)?.SetupParam(setupNumber, value);
        }

        public void ActiveSetup(int channel, byte[] setup)
        {
            GetByChannel(channel)?.ActiveSetup(setup);
        }

        public void DoProgramChange(int channel, int program)
        {
            var part = GetByChannel(channel);
            if (part != null)
            {
                part.Program = program;
            }
        }

        private Part GetByChannel(int channel)
        {
            partsByChannel.TryGetValue(channel, out Part part);

            // iMUSE v2 auto-allocates when channel is requested by MIDI
            // TODO: Are the some cases it doesn't? And should this only be done for v2?
            // In original iMUSE v2, auto-allocation happens from:
            // pt_start_note        HandleChannelMessage (via note-on)
            // pt_do_pgmch          HandleChannelMessage (via program change)
            // pt_do_active_dump    ActiveSetup
            // pt_load_setup        HandleImuseMessage
            // pt_do_param_adjust   SetupParam
            // pt_set_priority      SetPriority
            // pt_set_vol           SetVolume
            // pt_set_pan           HandleChannelMessage (via ctrl change)
            // pt_set_transpose     SetTranspose
            // pt_set_detune        HandleChannelMessage (via ctrl change)
            // pt_set_part_enable   SetEnabled
            // pt_set_modw          HandleChannelMessage (via ctrl change)
            // pt_set_sust          HandleChannelMessage (via ctrl change)
            // pt_set_pbend         HandleChannelMessage (via pbend message)
            // pt_set_pbr           HandleChannelMessage (via ctrl change)
            // pt_set_reverb        HandleChannelMessage (via ctrl change)
            if (part == null)
            {
                logger.Verbose($"Auto-allocating part for channel {channel}");
                part = manager.AllocPart(player, new DefaultPartAllocation(channel, driver.DefaultReverb));
            }

            return part;
        }

        public void Add(Part item)
        {
            parts.Add(item);
            partsByChannel.Add(item.InputChannel, item);
        }

        public bool Remove(Part item)
        {
            var exists = parts.Remove(item);
            if (exists)
            {
                partsByChannel.Remove(item.InputChannel);
            }
            return exists;
        }

        public bool Contains(Part item)
        {
            return parts.Contains(item);
        }

        public IEnumerator<Part> GetEnumerator()
        {
            return parts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
