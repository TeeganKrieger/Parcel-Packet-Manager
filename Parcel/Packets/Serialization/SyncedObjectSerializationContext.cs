using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parcel.Packets
{
    internal class SyncedObjectSerializationContext
    {
        public ParcelClient Client { get; private set; }
        public ParcelServer Server { get; private set; }
        public SyncedObjectSerializationMode SerializationMode { get; private set; }

        public bool HasClient => this.Client != null;
        public bool HasServer => this.Server != null;

        public SyncedObjectSerializationContext(ParcelClient client, SyncedObjectSerializationMode serializationMode)
        {
            this.Client = client;
            this.SerializationMode = serializationMode;
        }

        public SyncedObjectSerializationContext(ParcelServer server, SyncedObjectSerializationMode serializationMode)
        {
            this.Server = server;
            this.SerializationMode = serializationMode;
        }
    }
}
