using System;

namespace Parcel.Serialization
{
    internal class ObjectArraySerializer : Serializer
    {
        public override object Deserialize(ByteReader reader)
        {
            return reader.ReadObjectArray();
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            writer.Write((object[])obj);
        }

        public override bool CanSerialize(Type type)
        {
            return type.IsArray;
        }
    }
}
