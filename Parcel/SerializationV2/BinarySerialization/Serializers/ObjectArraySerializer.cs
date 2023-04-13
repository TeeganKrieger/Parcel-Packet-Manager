using System;

namespace Parcel.Serialization.Binary
{
    internal class ObjectArraySerializer : SerializerV2, IBinarySerializer
    {
        public override object Deserialize(DataReader reader)
        {
            return reader.ReadObjectArray();
        }

        public override void Serialize(DataWriter writer, object obj)
        {
            writer.Write((object[])obj);
        }

        public override bool CanSerialize(Type type)
        {
            return type.IsArray;
        }
    }
}
