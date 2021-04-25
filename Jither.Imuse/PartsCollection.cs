using Jither.Imuse.Drivers;
using Jither.Imuse.Messages;
using Jither.Midi.Helpers;
using Jither.Midi.Messages;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Jither.Imuse
{
    public struct SustainedNote
    {
        public int Channel { get; }
        public int Key { get; }

        public SustainedNote(int channel, int key)
        {
            Channel = channel;
            Key = key;
        }

        public override string ToString()
        {
            return $"Channel {Channel}, Key {MidiHelper.NoteNumberToName(Key)} ({Key})";
        }
    }

    public class PartsCollection : IReadOnlyList<Part>
    {
        private readonly Driver driver;
        private readonly List<Part> parts = new();
        private readonly Dictionary<int, Part> partsByChannel = new();

        public int Count => parts.Count;

        public Part this[int index] => parts[index];

        public PartsCollection(Driver driver)
        {
            this.driver = driver;
        }

        public void HandleEvent(ChannelMessage message)
        {
            GetByChannel(message.Channel)?.HandleEvent(message);
        }

        public void HandleEvent(ImuseMessage message)
        {
            GetByChannel(message.Channel)?.HandleEvent(message);
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
            GetByChannel(channel)?.SetPriorityOffset(priority);
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
            GetByChannel(channel)?.SetVolume(volume);
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
                    part.SetTranspose(Math.Clamp(interval + part.Transpose, -7, 7));
                }
                else
                {
                    part.SetTranspose(interval);
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
            GetByChannel(channel)?.SetEnabled(enabled);
        }

        public void SetupParam(int channel, int setupNumber, int value)
        {
            GetByChannel(channel)?.SetupParam(setupNumber, value);
        }

        public void ActiveSetup(int channel, byte[] setup)
        {
            GetByChannel(channel)?.ActiveSetup(setup);
        }

        public void StoredSetup(int channel, int setupNumber, byte[] setup)
        {
            GetByChannel(channel)?.StoredSetup(setupNumber, setup);
        }

        public void DoProgramChange(int channel, int program)
        {
            GetByChannel(channel)?.DoProgramChange(program);
        }

        private Part GetByChannel(int channel)
        {
            partsByChannel.TryGetValue(channel, out Part part);
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
