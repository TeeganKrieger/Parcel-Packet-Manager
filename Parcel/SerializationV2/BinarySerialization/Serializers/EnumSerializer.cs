using System;

namespace Parcel.Serialization.Binary
{
    internal class EnumSerializer : SerializerV2, IBinarySerializer
    {
        public override bool CanSerialize(Type type)
        {
            return type.IsEnum;
        }

        public override object Deserialize(DataReader reader)
        {
            return reader.ReadEnum(this.ObjectCache.Type);
        }

        public override void Serialize(DataWriter writer, object obj)
        {
            writer.Write((Enum)obj);
        }
    }
}
