using Parcel.Serialization;
using System.Collections.Concurrent;

namespace Parcel.Packets
{
    internal static class DataWriterExtensions
    {
        private static ConcurrentDictionary<DataWriter, ParcelClient> AttachedClients = new ConcurrentDictionary<DataWriter, ParcelClient>();
        private static ConcurrentDictionary<DataWriter, ParcelServer> AttachedServers = new ConcurrentDictionary<DataWriter, ParcelServer>();

        public static bool TryAttachClient(this DataWriter dataWriter, ParcelClient client)
        {
            return DataWriterExtensions.AttachedClients.TryAdd(dataWriter, client);
        }

        public static bool TryAttachServer(this DataWriter dataWriter, ParcelServer server)
        {
            return DataWriterExtensions.AttachedServers.TryAdd(dataWriter, server);
        }

        public static bool TryGetClient(this DataWriter dataWriter, out ParcelClient client)
        {
            return DataWriterExtensions.AttachedClients.TryGetValue(dataWriter, out client);
        }

        public static bool TryGetServer(this DataWriter dataWriter, out ParcelServer server)
        {
            return DataWriterExtensions.AttachedServers.TryGetValue(dataWriter, out server);
        }

        public static bool TryDetatchClient(this DataWriter dataWriter)
        {
            return DataWriterExtensions.AttachedClients.TryRemove(dataWriter, out _);
        }

        public static bool TryDetatchServer(this DataWriter dataWriter)
        {
            return DataWriterExtensions.AttachedServers.TryRemove(dataWriter, out _);
        }

    }
}
