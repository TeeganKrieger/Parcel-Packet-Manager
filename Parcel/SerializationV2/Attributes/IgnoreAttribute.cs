using System;

namespace Parcel.Serialization
{
    /// <summary>
    /// Specifies that a member of a class should be ignored during serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : Attribute
    {
    }
}
