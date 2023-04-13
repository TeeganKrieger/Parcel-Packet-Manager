using Parcel.DataStructures;
using Parcel.Lib;
using Parcel.Networking;
using Parcel.Serialization;
using Parcel.Serialization.Binary;
using System;
using System.Collections.Generic;

namespace Parcel.Packets
{
    /// <summary>
    /// Serializer for SyncedObjects.
    /// </summary>
    /// <remarks>
    /// This serializer diverges from the standard serializer conventions quite heavily, as a result, it is not compatible with the 
    /// SerializerResolver. 
    /// </remarks>
    public sealed class BinarySyncedObjectSerializer : SyncedObjectSerializer, IBinarySerializer
    {

        public SyncedObject Deserialize(DataReader reader, Peer sender, out Dictionary<string, SyncedObject.PropertyChanges> changes)
        {
            //auto reject will be set to true if the sender and owner of an existing SyncedObject do not match.
            //Packet will then reject all changes but still be read to the end.
            bool reject = false;
            TypeHashCode typeHash = reader.ReadObject<TypeHashCode>();
            SyncedObjectID soid = (SyncedObjectID)reader.ReadUInt();

            ObjectCache cache = ObjectCache.FromHash(typeHash);
            object[] getterArgs = new object[0];
            object[] setterArgs = new object[1];

            //Try to get existing SyncedObject if it exists, otherwise create a new instance.
            //Also handle autoReject conditions
            SyncedObject syncedObject;
            changes = new Dictionary<string, SyncedObject.PropertyChanges>();
            if (this._isServer)
            {
                this._server.TryGetSyncedObject(soid, out syncedObject);
                //If no SyncedObject exists to update on the server, reject this packet.
                if (syncedObject == null)
                    reject = true;
                //If SyncedObject exists but the owner and sender are not the same, reject this packet.
                else if (syncedObject.Owner != sender)
                    reject = true;
            }
            else
            {
                this._client.TryGetSyncedObject(soid, out syncedObject);

                //If no SyncedObject exists to update on the client, reject this packet.
                if (syncedObject == null)
                    reject = true;
                //If sender is not the server, reject this packet.
                if (sender != this._client.Remote)
                    reject = true;
            }

            if (reject)
            {
                syncedObject = null;
                //Loop until we find a 0. Throw away all properties found.
                uint propertyHash = reader.ReadUInt();
                while (propertyHash != 0U)
                {
                    ObjectProperty property = cache.GetProperty(propertyHash);
                    reader.SerializerResolver.GetSerializer(property.Type).Deserialize(reader);
                    propertyHash = reader.ReadUInt();
                }
            }
            else
            {
                //Force SyncedObject to ignore property updates
                syncedObject.DontSync = true;

                //Loop until we find a 0. Process each property.
                uint propertyHash = reader.ReadUInt();
                while (propertyHash != 0U)
                {
                    ObjectProperty property = cache.GetProperty(propertyHash);
                    setterArgs[0] = reader.SerializerResolver.GetSerializer(property.Type).Deserialize(reader);

                    object previousValue = property.Getter.Invoke(syncedObject, getterArgs);
                    property.Setter.Invoke(syncedObject, setterArgs);
                    object currentValue = property.Getter.Invoke(syncedObject, getterArgs);

                    if (previousValue == null ? currentValue != null : !previousValue.Equals(currentValue))
                    {
                        changes.Add(property.Name, new SyncedObject.PropertyChanges(previousValue, currentValue));
                        //Make changes on server instance of packet so that packet redirection works
                        if (this._isServer)
                            (PacketCacheHelper.GetReliability(property) == Reliability.Reliable ? syncedObject.ReliablePropertiesToSync
                                : syncedObject.UnreliablePropertiesToSync).TryAdd(property.NameHash);
                    }

                    propertyHash = reader.ReadUInt();
                }

                //Allow SyncedObject to handle property updates again
                syncedObject.DontSync = false;
            }

            return syncedObject;
        }

        public SyncedObject DeserializeAll(DataReader reader)
        {
            TypeHashCode typeHash = reader.ReadObject<TypeHashCode>();
            SyncedObjectID soid = (SyncedObjectID)reader.ReadUInt();

            ObjectCache cache = ObjectCache.FromHash(typeHash);
            object[] getterArgs = new object[0];
            object[] setterArgs = new object[1];

            //Try to get existing SyncedObject if it exists, otherwise create a new instance.
            //Also handle autoReject conditions
            SyncedObject syncedObject = (SyncedObject)Create.New(cache.Type);

            //Force SyncedObject to ignore property updates
            syncedObject.DontSync = true;

            //Loop until we find a 0. Process each property.
            uint propertyHash = reader.ReadUInt();
            while (propertyHash != 0U)
            {
                ObjectProperty property = cache.GetProperty(propertyHash);
                setterArgs[0] = reader.SerializerResolver.GetSerializer(property.Type).Deserialize(reader);

                property.Setter.Invoke(syncedObject, setterArgs);

                propertyHash = reader.ReadUInt();
            }

            //Allow SyncedObject to handle property updates again
            syncedObject.DontSync = false;

            return syncedObject;
        }

        public bool WillSerialize(SyncedObject syncedObject, Reliability reliability)
        {
            ConcurrentHashSet<uint> propertiesToUpdate = reliability == Reliability.Reliable ? syncedObject.ReliablePropertiesToSync : syncedObject.UnreliablePropertiesToSync;

            if (propertiesToUpdate.Count == 0)
                return false;
            return true;
        }

        public void Serialize(DataWriter writer, SyncedObject syncedObject, Reliability reliability)
        {
            ConcurrentHashSet<uint> propertiesToUpdate = reliability == Reliability.Reliable ? syncedObject.ReliablePropertiesToSync : syncedObject.UnreliablePropertiesToSync;

            ObjectCache cache = ObjectCache.FromType(syncedObject.GetType());
            object[] getterArgs = new object[0];

            writer.Write(cache.HashCode);

            //Writer SyncedObjectID
            uint soid = syncedObject.ID;
            writer.Write(soid);

            //Write AlwaysSerialize properties
            using (IEnumerator<ObjectProperty> enumerator = cache.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ObjectProperty property = enumerator.Current;

                    if (!PacketCacheHelper.WillAlwaysSerialize(property))
                        continue;

                    if (PacketCacheHelper.GetReliability(property) == reliability)
                    {
                        writer.Write(property.NameHash);
                        writer.SerializerResolver.GetSerializer(property.Type).Serialize(writer, property.Getter.Invoke(syncedObject, getterArgs));
                    }
                }
            }

            //Write properties that have changed
            foreach (uint propertyHash in propertiesToUpdate)
            {
                ObjectProperty property = cache.GetProperty(propertyHash);
                if (PacketCacheHelper.GetReliability(property) == reliability)
                {
                    writer.Write(property.NameHash);
                    writer.SerializerResolver.GetSerializer(property.Type).Serialize(writer, property.Getter.Invoke(syncedObject, getterArgs));
                }
            }
            propertiesToUpdate.Clear();

            writer.Write(0U);
        }

        

        public override void Serialize(DataWriter writer, object obj)
        {
            SyncedObject syncedObject = (SyncedObject)obj;

            switch (this.GetSyncedObjectSerializationMode(syncedObject))
            {
                case SyncedObjectSerializationMode.All:

                    break;
                case SyncedObjectSerializationMode.Reliable:

                    break;
                case SyncedObjectSerializationMode.Unreliable:

                    break;
                case SyncedObjectSerializationMode.Reference:

                    break;
            }
        }

        public override object Deserialize(DataReader reader)
        {

        }

        public override bool CanSerialize(Type type)
        {
            return typeof(SyncedObject).IsAssignableFrom(type);
        }

        public void SerializeAllProperties(DataWriter writer, SyncedObject syncedObject)
        {
            ObjectCache cache = ObjectCache.FromType(syncedObject.GetType());
            object[] getterArgs = new object[0];

            writer.Write(cache.HashCode);

            //Writer SyncedObjectID
            uint soid = syncedObject.ID;
            writer.Write(soid);

            //Write AlwaysSerialize properties
            using (IEnumerator<ObjectProperty> enumerator = cache.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ObjectProperty property = enumerator.Current;

                    writer.Write(property.NameHash);
                    writer.SerializerResolver.GetSerializer(property.Type).Serialize(writer, property.Getter.Invoke(syncedObject, getterArgs));
                }
            }

            writer.Write(0U);
        }

        public void SerializeReliableProperties()
        {

        }

        public void SerializeUnreliableProperties()
        {

        }

        public void SerializeReference()
        {

        }

    }
}
