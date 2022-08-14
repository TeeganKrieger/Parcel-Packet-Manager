
namespace Parcel.Networking
{
    /// <summary>
    /// Represents the reliability of a <see cref="Parcel.Packets.Packet">Packet</see> or <see cref="Parcel.Packets.SyncedObject">SyncedObject</see> property. 
    /// </summary>
    public enum Reliability
    {
        /// <summary>
        /// Indicates that a <see cref="Parcel.Packets.Packet">Packet</see> or <see cref="Parcel.Packets.SyncedObject">SyncedObject</see> property is unreliable.
        /// </summary>
        /// <remarks>
        /// Unreliable packets or properties are <see langword="not"/> guaranteed to be received by remote users.
        /// </remarks>
        Unreliable,

        /// <summary>
        /// Indicates that a <see cref="Parcel.Packets.Packet">Packet</see> or <see cref="Parcel.Packets.SyncedObject">SyncedObject</see> property is reliable.
        /// </summary>
        /// <remarks>
        /// Reliable packets or properties are guaranteed to be received in sending order by remote users.
        /// </remarks>
        Reliable,
    }
}
