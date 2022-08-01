using System;

namespace Parcel.Serialization
{
    /// <summary>
    /// Specifies that a member of a class should be encoded during serialization.
    /// </summary>
    /// <remarks>
    /// During serialization, only members whose values have changed since the previous serialization will be serialized.
    /// If you wish for a member to always be serialized, use the AlwaysEncode attribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class SerializeAttribute : Attribute
    {
    }
}
