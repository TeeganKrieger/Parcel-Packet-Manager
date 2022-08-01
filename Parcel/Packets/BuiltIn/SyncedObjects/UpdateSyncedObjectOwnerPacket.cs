using Parcel.Serialization;

namespace Parcel.Packets
{
    /// <summary>
    /// Packet that handles logic for updating the owner of a <see cref="Packets.SyncedObject"/> instances.
    /// </summary>
    [OptIn]
    [Reliable]
    internal sealed class UpdateSyncedObjectOwnerPacket : ServerToClientPacket
    {
        /// <summary>
        /// The <see cref="Packets.SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> whose owner is being updated.
        /// </summary>
        [Serialize]
        private SyncedObjectID SyncedObjectID { get; set; }

        /// <summary>
        /// The new owner of the <see cref="SyncedObject"/>.
        /// </summary>
        [Serialize]
        private Peer NewOwner { get; set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Default constructor for use by <see cref="Parcel.Lib.Create">Create</see>.
        /// </summary>
        private UpdateSyncedObjectOwnerPacket()
        {

        }

        /// <summary>
        /// Construct a new instance of UpdateSyncedObjectOwnerPacket.
        /// </summary>
        /// <param name="syncedObjectID">The <see cref="Packets.SyncedObjectID">ID</see> of the <see cref="SyncedObject"/> whose owner is being updated.</param>
        /// <param name="newOwner">The new owner of the <see cref="SyncedObject"/>.</param>
        public UpdateSyncedObjectOwnerPacket(SyncedObjectID syncedObjectID, Peer newOwner)
        {
            this.SyncedObjectID = syncedObjectID;
            this.NewOwner = newOwner;
        }

        #endregion


        #region SYNCEDOBJECT

        /// <inheritdoc/>
        protected internal override void OnReceive()
        {
            this.Client.UpdateSyncedObjectOwner(SyncedObjectID, NewOwner);
        }

        #endregion

    }
}
