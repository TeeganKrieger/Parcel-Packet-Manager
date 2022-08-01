using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization
{
    internal class EnumSerializer : Serializer
    {
        public override bool CanSerialize(Type type)
        {
            return type.IsEnum;
        }

        public override object Deserialize(ByteReader reader)
        {
            return reader.ReadEnum(ObjectCache.Type);
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            writer.Write((Enum)obj);
        }
    }
}
