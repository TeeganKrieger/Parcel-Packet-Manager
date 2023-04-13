using Parcel.Serialization;
using System.Collections.Concurrent;

namespace Parcel.Packets
{
    internal static class DataReaderExtensions
    {
        private static ConcurrentDictionary<DataReader, ParcelClient> AttachedClients = new ConcurrentDictionary<DataReader, ParcelClient>();
        private static ConcurrentDictionary<DataReader, ParcelServer> AttachedServers = new ConcurrentDictionary<DataReader, ParcelServer>();

        public static bool TryAttachClient(this DataReader dataReader, ParcelClient client)
        {
            return DataReaderExtensions.AttachedClients.TryAdd(dataReader, client);
        }

        public static bool TryAttachServer(this DataReader dataReader, ParcelServer server)
        {
            return DataReaderExtensions.AttachedServers.TryAdd(dataReader, server);
        }

        public static bool TryGetClient(this DataReader dataReader, out ParcelClient client)
        {
            return DataReaderExtensions.AttachedClients.TryGetValue(dataReader, out client);
        }

        public static bool TryGetServer(this DataReader dataReader, out ParcelServer server)
        {
            return DataReaderExtensions.AttachedServers.TryGetValue(dataReader, out server);
        }

        public static bool TryDetatchClient(this DataReader dataReader)
        {
            return DataReaderExtensions.AttachedClients.TryRemove(dataReader, out _);
        }

        public static bool TryDetatchServer(this DataReader dataReader)
        {
            return DataReaderExtensions.AttachedServers.TryRemove(dataReader, out _);
        }
    }
}
