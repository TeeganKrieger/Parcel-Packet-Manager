using System;

namespace Parcel
{
    /// <summary>
    /// Specifies that a property within a <see cref="Parcel.Packets.SyncedObject">SyncedObject</see> should always be 
    /// serialized when the object is serialized.
    /// </summary>
    /// <remarks>
    /// Under normal circumstances, a SyncedObject's properties will only serialize if they have changed. This attribute
    /// allows for a property to always be included in serialization, regardless of whether it changed or not.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AlwaysSerializeAttribute : Attribute
    {
    }
}
