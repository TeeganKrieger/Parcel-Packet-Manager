using System;

namespace Parcel.Serialization
{
    /// <summary>
    /// Specifies that during serialization, a class should opt-out members from serialization using the Ignore Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OptOutAttribute : Attribute
    {
    }
}
