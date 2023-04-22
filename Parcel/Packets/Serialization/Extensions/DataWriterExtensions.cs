using Parcel.Serialization;
using System.Collections.Concurrent;

namespace Parcel.Packets
{
    internal static class DataWriterExtensions
    {
        private static ConcurrentDictionary<DataWriter, SyncedObjectSerializationContext> AttachedContexts = new ConcurrentDictionary<DataWriter, SyncedObjectSerializationContext>();

        public static bool TryAttachSyncedObjectSerializationContext(this DataWriter dataWriter, ParcelClient client, SyncedObjectSerializationMode serializationMode)
        {
            return DataWriterExtensions.AttachedContexts.TryAdd(dataWriter, new SyncedObjectSerializationContext(client, serializationMode));
        }

        public static bool TryAttachSyncedObjectSerializationContext(this DataWriter dataWriter, ParcelServer server, SyncedObjectSerializationMode serializationMode)
        {
            return DataWriterExtensions.AttachedContexts.TryAdd(dataWriter, new SyncedObjectSerializationContext(server, serializationMode));
        }

        public static bool TryGetSyncedObjectSerializationContext(this DataWriter dataWriter, out SyncedObjectSerializationContext context)
        {
            return DataWriterExtensions.AttachedContexts.TryGetValue(dataWriter, out context);
        }

        public static bool TryDetatchSyncedObjectSerializationContext(this DataWriter dataWriter, out SyncedObjectSerializationContext context)
        {
            return DataWriterExtensions.AttachedContexts.TryRemove(dataWriter, out context);
        }
    }
}
