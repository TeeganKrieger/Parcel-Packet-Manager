using Parcel.Networking;
using Parcel.Serialization;

namespace Parcel.Packets
{
    /// <summary>
    /// Contains a collection of useful methods for working with Packet ObjectCaches.
    /// </summary>
    public static class PacketCacheHelper
    {

        /// <summary>
        /// Get the <see cref="Reliability"/> of a <see cref="Packet"/>.
        /// </summary>
        /// <param name="objectCache">The <see cref="ObjectCache"/> of the <see cref="Packet"/>.</param>
        /// <returns>The <see cref="Reliability"/> of the <see cref="Packet"/>.</returns>
        public static Reliability GetReliability(ObjectCache objectCache)
        {
            return objectCache.GetCustomAttribute<UnreliableAttribute>() != null ? Reliability.Unreliable : Reliability.Reliable;
        }

        /// <summary>
        /// Get the <see cref="Reliability"/> of a Property.
        /// </summary>
        /// <param name="objectProperty">The Property of the <see cref="SyncedObject"/>.</param>
        /// <returns>The <see cref="Reliability"/> of the Property.</returns>
        public static Reliability GetReliability(ObjectProperty objectProperty)
        {
            return objectProperty.GetCustomAttribute<UnreliableAttribute>() != null ? Reliability.Unreliable : Reliability.Reliable;
        }

        /// <summary>
        /// Get whether a Property will <see cref="AlwaysSerializeAttribute">Always Serialize</see>
        /// </summary>
        /// <param name="objectProperty">The Property of the <see cref="SyncedObject"/>.</param>
        /// <returns><see langword="true"/> if the Property has the <see cref="AlwaysSerializeAttribute"/>; otherwise, <see langword="false"/>.</returns>
        public static bool WillAlwaysSerialize(ObjectProperty objectProperty)
        {
            return objectProperty.GetCustomAttribute<AlwaysSerializeAttribute>() != null;
        }

        /// <summary>
        /// Get the direction in which an <see cref="ObjectProcedure"/> is allowed to execute.
        /// </summary>
        /// <param name="objectProcedure">The procedure to check.</param>
        /// <returns>The direction in which an <see cref="ObjectProcedure"/> is allowed to execute.</returns>
        public static RPCDirection GetRPCDirection(ObjectProcedure objectProcedure)
        {
            return objectProcedure.GetCustomAttribute<RPCAttribute>()?.Direction ?? RPCDirection.ServerToClient;
        }

    }
}
