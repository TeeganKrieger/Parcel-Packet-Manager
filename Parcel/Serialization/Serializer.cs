using System;

namespace Parcel.Serialization
{
    /// <summary>
    /// Base class for all Serializers.
    /// </summary>
    /// <remarks>
    /// Serializers are responsible for converting objects into binary representations as well as converting binary representations back into objects.
    /// </remarks>
    public abstract class Serializer : ICloneable
    {
        /// <summary>
        /// The <see cref="ObjectCache"/> associated with this serializer.
        /// </summary>
        public ObjectCache ObjectCache { get; internal set; }

        /// <summary>
        /// Serialize an object.
        /// </summary>
        /// <param name="writer">The <see cref="ByteWriter"/> the object is written to.</param>
        /// <param name="obj">The object being serialized.</param>
        public abstract void Serialize(ByteWriter writer, object obj);

        /// <summary>
        /// Deserialize an object.
        /// </summary>
        /// <param name="reader">The <see cref="ByteReader"/> to read the object from.</param>
        /// <returns>The object that was deserialized.</returns>
        public abstract object Deserialize(ByteReader reader);

        /// <summary>
        /// Checks if a type is able to be serialized using this Serializer.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the Type can be serialized; otherwise, <see langword="false"/>.</returns>
        public abstract bool CanSerialize(Type type);

        /// <summary>
        /// Clone this serializer.
        /// </summary>
        /// <returns>A Memberwise clone of this serializer.</returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
