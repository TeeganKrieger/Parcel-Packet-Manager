using System;

namespace Parcel.Serialization
{
    /// <summary>
    /// Specifies that during serialization, a class should opt-in members for serialization using the Encode Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class OptInAttribute : Attribute
    {
    }
}
