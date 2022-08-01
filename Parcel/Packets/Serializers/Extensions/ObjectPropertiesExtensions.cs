using Parcel.Serialization;

namespace Parcel.Packets
{
    /// <summary>
    /// Extension methods for <see cref="ObjectProperty"/>.
    /// </summary>
    public static class ObjectPropertiesExtensions
    {
        /// <summary>
        /// Get the <see cref="Reliability"/> of a Property.
        /// </summary>
        /// <param name="objectProperty">The Property of the <see cref="SyncedObject"/>.</param>
        /// <returns>The <see cref="Reliability"/> of the Property.</returns>
        public static Reliability GetReliability(this ObjectProperty objectProperty)
        {
            return objectProperty.GetCustomAttribute<UnreliableAttribute>() != null ? Reliability.Unreliable : Reliability.Reliable;
        }

        /// <summary>
        /// Get whether a Property will <see cref="AlwaysSerializeAttribute">Always Serialize</see>
        /// </summary>
        /// <param name="objectProperty">The Property of the <see cref="SyncedObject"/>.</param>
        /// <returns><see langword="true"/> if the Property has the <see cref="AlwaysSerializeAttribute"/>; otherwise, <see langword="false"/>.</returns>
        public static bool WillAlwaysSerialize(this ObjectProperty objectProperty)
        {
            return objectProperty.GetCustomAttribute<AlwaysSerializeAttribute>() != null;
        }
    }
}
