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

        public void SetPriority(int priority, int channel)
        {
            if (channel == Part.OmniChannel)
            {
                // Property evaluation automatically reflects new value
                return;
            }
            foreach (var part in parts)
            {
                if (part.Channel == channel)
                {
                    part.SetPriorityOffset(priority);
                }
            }
        }

        public void SetVolume(int volume, int channel)
        {
            foreach (var part in parts)
            {
                if (channel == Part.OmniChannel)
                {
                    // Property evaluation automatically reflects new value, so just call driver
                    driver.SetVolume(part);
                }
                else if (part.Channel == channel)
                {
                    part.SetVolume(volume);
                }
            }
        }

        public void SetPan(int pan, int channel)
        {
            foreach (var part in parts)
            {
                if (channel == Part.OmniChannel)
                {
                    // Property evaluation automatically reflects new value, so just call driver
                    driver.SetPan(part);
                }
                else if (part.Channel == channel)
                {
                    part.SetPan(pan);
                }
            }
        }

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
                    continue;
                }

                if (channel == Part.OmniChannel)
                {
                    driver.SetPitchOffset(part);
                }
                else if (part.Channel == channel)
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

        public void SetDetune(int detune, int channel)
        {
            foreach (var part in parts)
            {
                if (channel == Part.OmniChannel)
                {
                    driver.SetPitchOffset(part);
                }
                else if (part.Channel == channel)
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
