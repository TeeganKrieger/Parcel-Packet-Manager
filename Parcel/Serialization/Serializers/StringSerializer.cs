using System;
using System.Runtime.InteropServices;

namespace Parcel.Serialization
{
    internal class StringSerializer : Serializer
    {
        public override object Deserialize(ByteReader reader)
        {
            return reader.ReadString();
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            writer.Write((string)obj);
        }

        public override bool CanSerialize(Type type)
        {
            return type == typeof(string);
        }
    }
}
