using System;

namespace Parcel.Serialization.Binary
{
    internal class StringSerializer : SerializerV2, IBinarySerializer
    {
        public override object Deserialize(DataReader reader)
        {
            return reader.ReadString();
        }

        public override void Serialize(DataWriter writer, object obj)
        {
            writer.Write((string)obj);
        }

        public override bool CanSerialize(Type type)
        {
            return type == typeof(string);
        }
    }
}
