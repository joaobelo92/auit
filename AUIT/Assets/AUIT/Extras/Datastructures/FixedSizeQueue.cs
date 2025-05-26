using System;
using System.Collections;
using System.Collections.Generic;

namespace AUIT.Extras.Datastructures
{
    public class FixedSizeQueue<T> : IEnumerable<T>
    {
        private readonly Queue<T> _queue = new ();
        private int Capacity { get; }

        public FixedSizeQueue(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0");
            Capacity = capacity;
        }

        public void Enqueue(T item)
        {
            if (_queue.Count == Capacity)
            {
                _queue.Dequeue();
            }
            _queue.Enqueue(item);
        }
    
        public T Dequeue() => _queue.Dequeue();
        public T Peek() => _queue.Peek();
        public int Count => _queue.Count;
    
        public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
