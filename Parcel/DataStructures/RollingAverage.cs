using System;
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
            get => this._total / (double)Math.Max(1, this._queue.Count);
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
            if (this._queue.Count == this._capacity)
            {
                this._total -= this._queue.Dequeue();
            }
            this._queue.Enqueue(value);
            this._total += value;
        }
    }
}
