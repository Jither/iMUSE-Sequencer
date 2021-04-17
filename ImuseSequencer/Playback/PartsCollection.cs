using ImuseSequencer.Drivers;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ImuseSequencer.Playback
{
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

        public void StartNote(int channel, int note, int velocity)
        {
            GetByChannel(channel)?.StartNote(note, velocity);
        }

        public void StopNote(int channel, int note)
        {
            GetByChannel(channel)?.StopNote(note);
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

        public void GetSustainNotes(HashSet<int> notes)
        {
            foreach (var part in parts)
            {
                part.GetSustainNotes(notes);
            }
        }

        public void DoProgramChange(int channel, int program)
        {
            GetByChannel(channel)?.DoProgramChange(program);
        }

        public void DoActiveDump(int channel, byte[] data)
        {
            GetByChannel(channel)?.DoActiveDump(data);
        }

        public void DoStoredDump(int program, byte[] data)
        {
            if (program < Roland.StoredSetupCount)
            {
                driver.DoStoredDump(program, data);
            }
        }

        public void LoadSetup(int channel, int program)
        {
            if (program < Roland.StoredSetupCount)
            {
                GetByChannel(channel)?.LoadSetup(program);
            }
        }

        public void DoParamAdjust(int channel, int param, int value)
        {
            GetByChannel(channel)?.DoParamAdjust(param, value);
        }

        /// <summary>
        /// Updates the priority offset of a given channel. If OMNI channel, does nothing: Normally OMNI
        /// would indicate to update the effective value of every channel to reflect a new priority on
        /// the player. But effective priority is always current in this implementation.
        /// </summary>
        public void SetPriority(int channel, int priority)
        {
            if (channel == Part.OmniChannel)
            {
                // Property evaluation automatically reflects new value
                return;
            }
            GetByChannel(channel)?.SetPriorityOffset(priority);
        }

        /// <summary>
        /// Updates the volume of a single channel or OMNI, and signals the new volume to the driver.
        /// </summary>
        public void SetVolume(int channel, int volume)
        {
            if (channel == Part.OmniChannel)
            {
                foreach (var part in parts)
                {
                    // Property evaluation automatically reflects new value, so just signal driver
                    driver.SetVolume(part);
                }
            }
            else
            {
                GetByChannel(channel)?.SetVolume(volume);
            }
        }

        /// <summary>
        /// Updates the pan of a single channel or OMNI, and signals the new pan to the driver.
        /// </summary>
        public void SetPan(int channel, int pan)
        {
            if (channel == Part.OmniChannel)
            {
                foreach (var part in parts)
                {
                    // Property evaluation automatically reflects new value, so just signal driver
                    driver.SetPan(part);
                }
            }
            else
            {
                GetByChannel(channel)?.SetPan(pan);
            }
        }

        /// <summary>
        /// Updates transposition of a single channel or OMNI, and signals the new pan to the driver.
        /// </summary>
        public void SetTranspose(int channel, int transpose, bool relative)
        {
            if (transpose < -24 || transpose > 24)
            {
                return;
            }

            if (channel == Part.OmniChannel)
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
            else
            {
                var part = GetByChannel(channel);
                if (part != null && !part.TransposeLocked)
                {
                    if (relative)
                    {
                        part.SetTranspose(Math.Clamp(transpose + part.Transpose, -7, 7));
                    }
                    else
                    {
                        part.SetTranspose(transpose);
                    }
                }
            }
        }

        /// <summary>
        /// Updates detune for a single channel or OMNI, and signals the new detune to the driver.
        /// </summary>
        /// <param name="detune"></param>
        /// <param name="channel"></param>
        public void SetDetune(int channel, int detune)
        {
            if (channel == Part.OmniChannel)
            {
                foreach (var part in parts)
                {
                    // Transpose, detune and pitch bend are combined for MT-32
                    driver.SetPitchOffset(part);
                }
            }
            else
            {
                GetByChannel(channel)?.SetDetune(detune);
            }
        }

        public void SetEnabled(int channel, bool enabled)
        {
            GetByChannel(channel)?.SetEnabled(enabled);
        }

        public void SetModWheel(int channel, int value)
        {
            GetByChannel(channel)?.SetModWheel(value);
        }

        public void SetSustain(int channel, int value)
        {
            GetByChannel(channel)?.SetSustain(value);
        }

        public void SetPitchbend(int channel, int value)
        {
            GetByChannel(channel)?.SetPitchbend(value);
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
