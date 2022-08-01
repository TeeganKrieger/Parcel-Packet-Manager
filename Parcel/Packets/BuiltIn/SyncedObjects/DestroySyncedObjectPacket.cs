using Parcel.Serialization;

namespace Parcel.Packets
{
    /// <summary>
    /// Packet that handles logic for destroying <see cref="Packets.SyncedObject"/> instances.
    /// </summary>
    [OptIn]
    [Reliable]
    internal sealed class DestroySyncedObjectPacket : ServerToClientPacket
    {
        /// <summary>
        /// The <see cref="Packets.SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> being destroyed.
        /// </summary>
        [Serialize]
        private SyncedObjectID SyncedObjectID { get; set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Default constructor for use by <see cref="Parcel.Lib.Create">Create</see>.
        /// </summary>
        private DestroySyncedObjectPacket()
        {

        }

        /// <summary>
        /// Construct a new instance of DestroySyncedObjectPacket with <paramref name="syncedObjectID"/>.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> being destroyed.</param>
        public DestroySyncedObjectPacket(SyncedObjectID syncedObjectID)
        {
            this.SyncedObjectID = syncedObjectID;
        }

        #endregion


        #region SYNCEDOBJECT

        /// <inheritdoc/>
        protected internal override void OnReceive()
        {
            this.Client.RemoveSyncedObject(SyncedObjectID);
        }

        #endregion

    }
}
