using Parcel.Networking;
using Parcel.Serialization;

namespace Parcel.Packets
{

    /// <summary>
    /// Extension methods for <see cref="ObjectCache"/>.
    /// </summary>
    public static class ObjectCacheExtensions
    {

        /// <summary>
        /// Get the <see cref="Reliability"/> of a <see cref="Packet"/>.
        /// </summary>
        /// <param name="objectCache">The <see cref="ObjectCache"/> of the <see cref="Packet"/>.</param>
        /// <returns>The <see cref="Reliability"/> of the <see cref="Packet"/>.</returns>
        public static Reliability GetReliability(this ObjectCache objectCache)
        {
            return objectCache.GetCustomAttribute<UnreliableAttribute>() != null ? Reliability.Unreliable : Reliability.Reliable;
        }
    }
}
