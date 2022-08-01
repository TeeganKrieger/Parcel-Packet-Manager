using System;

namespace Parcel
{
    /// <summary>
    /// Specifies that a <see cref="Parcel.Packets.Packet">Packet</see> or property within a 
    /// <see cref="Parcel.Packets.SyncedObject">SyncedObject</see> is considered unreliable.
    /// </summary>
    /// <remarks>
    /// When a packet or property is unreliable, this indicates that the packet or property is not guaranteed to be received by remote users.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class UnreliableAttribute : Attribute
    {

    }
}
