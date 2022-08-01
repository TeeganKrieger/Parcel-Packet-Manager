using Parcel.Serialization;
using System.Threading;

namespace Parcel.Packets
{
    /// <summary>
    /// Represents an ID of a <see cref="SyncedObject"/>.
    /// </summary>
    [OptIn]
    public struct SyncedObjectID
    {
        private static int NextID = 0;

        /// <summary>
        /// The uint representation of the ID.
        /// </summary>
        [Serialize]
        private uint ID { get; set; }


        #region CONSTURCTOR

        /// <summary>
        /// Construct a new SyncedObjectID struct.
        /// </summary>
        /// <param name="id">The underlying uint ID.</param>
        public SyncedObjectID(uint id)
        {
            this.ID = id;
        }

        #endregion


        #region STATIC METHODS

        /// <summary>
        /// Get the next SyncedObjectID in the sequence.
        /// </summary>
        /// <returns>A new SyncedObjectID struct.</returns>
        internal static SyncedObjectID Next()
        {
            Interlocked.Increment(ref NextID);
            unchecked
            {
                return new SyncedObjectID((uint)NextID);
            }
        }

        #endregion


        #region OVERRIDES

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is SyncedObjectID soid &&
                   this.ID == soid.ID;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (int)this.ID;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"SyncedObjectID({this.ID})";
        }

        #endregion


        #region OPERATORS

        /// <summary>
        /// Explicitly convert a uint to a SyncedObjectID.
        /// </summary>
        /// <param name="id">The uint to convert.</param>
        public static explicit operator SyncedObjectID(uint id)
        {
            return new SyncedObjectID(id);
        }

        /// <summary>
        /// Implicitly convert a SyncedObjectID to a uint.
        /// </summary>
        /// <param name="soid">The SyncedObjectID to convert.</param>
        public static implicit operator uint(SyncedObjectID soid)
        {
            return soid.ID;
        }

        /// <inheritdoc/>
        public static bool operator ==(SyncedObjectID left, SyncedObjectID right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(SyncedObjectID left, SyncedObjectID right)
        {
            return !left.Equals(right);
        }

        #endregion

    }
}
