using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jither.Midi.Sequencing
{
    public interface ISchedulable
    {
        public long Time { get; }
    }

    public class ScheduleQueue<T> where T:ISchedulable
    {
        private readonly LinkedList<List<T>> items = new();

        public long EarliestTime => IsEmpty ? throw new InvalidOperationException("ScheduleQueue is empty.") : items.First.Value[0].Time;

        public bool IsEmpty => items.Count == 0;

        public ScheduleQueue()
        {

        }

        public void Add(T item)
        {
            if (IsEmpty || item.Time > items.Last.Value[0].Time)
            {
                var slice = new List<T>
                {
                    item
                };
                items.AddLast(slice);
                return;
            }

            var node = items.Last;

            while (node.Previous != null && node.Value[0].Time > item.Time)
            {
                node = node.Previous;
            }

            if (node.Value[0].Time < item.Time)
            {
                // Need a new slice
                var slice = new List<T>
                {
                    item
                };
                items.AddAfter(node, slice);
            }
            else if (node.Value[0].Time > item.Time)
            {
                var slice = new List<T>
                {
                    item
                };
                items.AddBefore(node, slice);
            }
            else
            {
                // We have a slice at the right time already
                node.Value.Add(item);
            }
        }

        public void Clear()
        {
            items.Clear();
        }

        public List<T> PopEarliest()
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("ScheduleQueue is empty.");
            }
            var result = items.First.Value;
            items.RemoveFirst();
            return result;
        }
    }
}
