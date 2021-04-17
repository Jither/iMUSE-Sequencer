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

        public int Count => parts.Count;

        public Part this[int index] => parts[index];

        public PartsCollection(Driver driver)
        {
            this.driver = driver;
        }

        /// <summary>
        /// Updates the priority offset of a given channel. If OMNI channel, does nothing: Normally OMNI
        /// would indicate to update the effective value of every channel to reflect a new priority on
        /// the player. But effective priority is always current in this implementation.
        /// </summary>
        public void SetPriority(int priority, int channel)
        {
            if (channel == Part.OmniChannel)
            {
                // Property evaluation automatically reflects new value
                return;
            }
            foreach (var part in parts)
            {
                if (part.InputChannel == channel)
                {
                    part.SetPriorityOffset(priority);
                }
            }
        }

        /// <summary>
        /// Updates the volume of a single channel or OMNI, and signals the new volume to the driver.
        /// </summary>
        public void SetVolume(int volume, int channel)
        {
            foreach (var part in parts)
            {
                if (channel == Part.OmniChannel)
                {
                    // Property evaluation automatically reflects new value, so just signal driver
                    driver.SetVolume(part);
                }
                else if (part.InputChannel == channel)
                {
                    part.SetVolume(volume);
                }
            }
        }

        /// <summary>
        /// Updates the pan of a single channel or OMNI, and signals the new pan to the driver.
        /// </summary>
        public void SetPan(int pan, int channel)
        {
            foreach (var part in parts)
            {
                if (channel == Part.OmniChannel)
                {
                    // Property evaluation automatically reflects new value, so just signal driver
                    driver.SetPan(part);
                }
                else if (part.InputChannel == channel)
                {
                    part.SetPan(pan);
                }
            }
        }

        /// <summary>
        /// Updates transposition of a single channel or OMNI, and signals the new pan to the driver.
        /// </summary>
        public void SetTranspose(int transpose, bool relative, int channel)
        {
            if (transpose < -24 || transpose > 24)
            {
                return;
            }

            foreach (var part in parts)
            {
                if (part.TransposeLocked)
                {
                    // Percussion part - doesn't transpose
                    continue;
                }

                if (channel == Part.OmniChannel)
                {
                    // Transpose, detune and pitch bend are combined for MT-32
                    driver.SetPitchOffset(part);
                }
                else if (part.InputChannel == channel)
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
        public void SetDetune(int detune, int channel)
        {
            foreach (var part in parts)
            {
                if (channel == Part.OmniChannel)
                {
                    // Transpose, detune and pitch bend are combined for MT-32
                    driver.SetPitchOffset(part);
                }
                else if (part.InputChannel == channel)
                {
                    part.SetDetune(detune);
                }
            }
        }

        public void Add(Part item)
        {
            parts.Add(item);
        }

        public bool Remove(Part item)
        {
            return parts.Remove(item);
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
