//ConcurrentHashSet implementation provided by Ben Mosher on stackoverflow
//https://stackoverflow.com/a/11034999

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Parcel.DataStructures
{

    /// <summary>
    /// Represents a thread-safe set of values.
    /// </summary>
    /// <typeparam name="T">The type of elements in the hash set.</typeparam>
    internal sealed class ConcurrentHashSet<T> : IEnumerable<T>, IDisposable
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly HashSet<T> _hashSet = new HashSet<T>();


        #region ICOLLECTION IMPLEMENTATION
        
        /// <summary>
        /// Add an item to the set.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> is successfully added to the set; otherwise, <see langword="false"/>.</returns>
        public bool TryAdd(T item)
        {
            try
            {
                this._lock.EnterWriteLock();
                return this._hashSet.Add(item);
            }
            finally
            {
                if (this._lock.IsWriteLockHeld) this._lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Clear the set of all contents.
        /// </summary>
        public void Clear()
        {
            try
            {
                this._lock.EnterWriteLock();
                this._hashSet.Clear();
            }
            finally
            {
                if (this._lock.IsWriteLockHeld) this._lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Check if the set contains <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns><see langword="true"/> if the set contains <paramref name="item"/>; otherwise, <see langword="false"/>.</returns>
        public bool Contains(T item)
        {
            try
            {
                this._lock.EnterReadLock();
                return this._hashSet.Contains(item);
            }
            finally
            {
                if (this._lock.IsReadLockHeld) this._lock.ExitReadLock();
            }
        }

        /// <summary>
        /// Remove an item from the set.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> is removed from the set; otherwise, <see langword="false"/>.</returns>
        public bool TryRemove(T item)
        {
            try
            {
                this._lock.EnterWriteLock();
                return this._hashSet.Remove(item);
            }
            finally
            {
                if (this._lock.IsWriteLockHeld) this._lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get the number of items in the set.
        /// </summary>
        public int Count
        {
            get
            {
                try
                {
                    this._lock.EnterReadLock();
                    return this._hashSet.Count;
                }
                finally
                {
                    if (this._lock.IsReadLockHeld) this._lock.ExitReadLock();
                }
            }
        }
        
        #endregion


        #region IENUMERABLE

        ///<inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            try
            {
                _lock.EnterReadLock();
                foreach (T item in _hashSet)
                    yield return item;
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        ///<inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            try
            {
                _lock.EnterReadLock();
                foreach (T item in _hashSet)
                    yield return item;
            }
            finally
            {
                if (_lock.IsReadLockHeld) _lock.ExitReadLock();
            }
        }

        #endregion


        #region DISPOSE

        ///<inheritdoc/>
        public void Dispose()
        {
            if (_lock != null) _lock.Dispose();
        }

        #endregion
    }
}
