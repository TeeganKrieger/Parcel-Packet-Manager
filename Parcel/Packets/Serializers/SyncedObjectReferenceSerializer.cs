using Parcel.Lib;
using Parcel.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Packets
{
    internal class SyncedObjectReferenceSerializer : Serializer
    {
        private bool _isServer;
        private ParcelClient _client;
        private ParcelServer _server;

        public SyncedObjectReferenceSerializer(ParcelClient client)
        {
            this._client = client;
            this._isServer = false;
        }

        public SyncedObjectReferenceSerializer(ParcelServer server)
        {
            this._server = server;
            this._isServer = true;
        }

        public override bool CanSerialize(Type type)
        {
            return typeof(SyncedObject).IsAssignableFrom(type);
        }

        public override object Deserialize(ByteReader reader)
        {
            uint id = reader.ReadUInt();
            SyncedObjectID soid = new SyncedObjectID(id);

            if (this._isServer)
            {
                this._server.TryGetSyncedObject(soid, out SyncedObject obj);
                return obj;
            }
            else
            {
                this._client.TryGetSyncedObject(soid, out SyncedObject obj);
                return obj;
            }
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            SyncedObject so = (SyncedObject)obj;
            uint soid = so.ID;
            writer.Write(soid);
        }
    }
}
