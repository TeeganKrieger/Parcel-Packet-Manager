using System;

namespace Parcel
{
    /// <summary>
    /// Specifies that a <see cref="Parcel.Packets.Packet">Packet</see> or property within a 
    /// <see cref="Parcel.Packets.SyncedObject">SyncedObject</see> is considered reliable.
    /// </summary>
    /// <remarks>
    /// When a packet or property is reliable, this indicates that the packet or property is guaranteed to be received in order of sending by remote users.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class ReliableAttribute : Attribute
    {
        
    }
}
