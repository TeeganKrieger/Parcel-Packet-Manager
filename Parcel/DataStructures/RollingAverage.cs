using System.Collections.Generic;

namespace Parcel.DataStructures
{
    /// <summary>
    /// Represents a ever-changing average.
    /// </summary>
    internal sealed class RollingAverage
    {
        private int _capacity;
        private Queue<int> _queue;
        private long _total;

        /// <summary>
        /// Get the current average.
        /// </summary>
        public double Average
        {
            get => _total / (double)_queue.Count;
        }

        /// <summary>
        /// Construct a new RollingAverage.
        /// </summary>
        /// <param name="capacity">The maximum number of values allowed in the RollingAverage.</param>
        public RollingAverage(int capacity)
        {
            this._total = 0;
            this._capacity = capacity;
            this._queue = new Queue<int>();
        }

        /// <summary>
        /// Add <paramref name="value"/> to the rolling average.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(int value)
        {
            if (_queue.Count == _capacity)
            {
                _total -= _queue.Dequeue();
            }
            _queue.Enqueue(value);
            _total += value;
        }
    }
}
