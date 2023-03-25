namespace Parcel
{
    /// <summary>
    /// Represents the behavior for handling <see cref="Packets.SyncedObject">SyncedObjects</see> a server should perform when a user disconnects.
    /// </summary>
    public enum ServerDisconnectionBehavior
    {
        /// <summary>
        /// Indicates that a server should destroy a user's <see cref="Packets.SyncedObject">SyncedObjects</see> when they disconnect.
        /// </summary>
        DestroySyncedObjects,
        /// <summary>
        /// Indicates that a server should preserve a user's <see cref="Packets.SyncedObject">SyncedObjects</see> when they disconnect.
        /// </summary>
        PreserveSyncedObjects
    }
}
