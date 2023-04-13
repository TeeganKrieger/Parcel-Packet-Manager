using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcel.Packets
{
    public abstract class SyncedObjectSerializer : SerializerV2
    {
        protected bool IsClient { get; private set; }
        protected bool IsServer => !IsClient;

        protected ParcelClient Client { get; private set; }
        protected ParcelServer Server { get; private set; }
        
        internal void SetMetadata(ParcelClient client)
        {
            this.IsClient = true;
            this.Client = client;
        }

        internal void SetMetadata(ParcelServer server)
        {
            this.IsClient = false;
            this.Server = server;
        }

        protected SyncedObjectSerializationMode GetSyncedObjectSerializationMode(SyncedObject syncedObject)
        {
            return syncedObject.SerializationMode;
        }
    }
}
