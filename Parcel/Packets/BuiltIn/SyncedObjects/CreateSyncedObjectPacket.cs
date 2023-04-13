using Parcel.Serialization;

namespace Parcel.Packets
{

    /// <summary>
    /// Packet that handles logic for creating new <see cref="Packets.SyncedObject"/> instances.
    /// </summary>
    [OptIn]
    [Reliable]
    internal sealed class CreateSyncedObjectPacket : ServerToClientPacket
    {
        private static SyncedObjectSerializer SyncedObjectSerializer = new SyncedObjectSerializer((ParcelClient)null);

        /// <summary>
        /// The SyncedObject being created.
        /// </summary>
        [Ignore]
        private SyncedObject SyncedObject { get; set; }

        /// <summary>
        /// The serialized form of the <see cref="Packets.SyncedObject"/>.
        /// </summary>
        [Serialize]
        private byte[] RawSyncedObject { get; set; }


        #region CONSTRUCTOR

        /// <summary>
        /// Default constructor for use by <see cref="Parcel.Lib.Create">Create</see>.
        /// </summary>
        private CreateSyncedObjectPacket()
        {

        }

        /// <summary>
        /// Construct a new instance of CreateSyncedObjectPacket with <paramref name="syncedObject"/>.
        /// </summary>
        /// <param name="syncedObject">The <see cref="Packets.SyncedObject"/> being created.</param>
        public CreateSyncedObjectPacket(SyncedObject syncedObject)
        {
            this.SyncedObject = syncedObject;
        }

        #endregion


        #region SYNCEDOBJECT

        /// <inheritdoc/>
        protected internal override void OnSend()
        {
            DataWriter writer = this.IsClient ? this.Client.NetworkSettings.SerializerResolver.NewDataWriter() : this.Server.NetworkSettings.SerializerResolver.NewDataWriter();
            SyncedObjectSerializer.SerializeAll(writer, SyncedObject);
            this.RawSyncedObject = writer.Data;
        }

        /// <inheritdoc/>
        protected internal override void OnReceive()
        {
            DataReader reader = this.IsClient ? this.Client.NetworkSettings.SerializerResolver.NewDataReader(RawSyncedObject) : this.Server.NetworkSettings.SerializerResolver.NewDataReader(RawSyncedObject);
            SyncedObject = SyncedObjectSerializer.DeserializeAll(reader);
            this.Client.AddSyncedObject(SyncedObject);
        }

        #endregion
    }
}
