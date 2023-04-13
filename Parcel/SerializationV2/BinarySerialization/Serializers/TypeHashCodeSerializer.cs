using System;

namespace Parcel.Serialization.Binary
{
    internal class TypeHashCodeSerializer : SerializerV2, IBinarySerializer
    {
        public override bool CanSerialize(Type type)
        {
            return type.Equals(typeof(TypeHashCode));
        }

        public override object Deserialize(DataReader reader)
        {
            ulong hashcode = reader.ReadULong();
            TypeHashCode temp = new TypeHashCode(hashcode, null);

            TypeHashCode[] genericArgs = null;
            if (temp.IsGenericType)
            {
                genericArgs = new TypeHashCode[temp.GenericArgumentCount];
                for (int i = 0; i < temp.GenericArgumentCount; i++)
                {
                    genericArgs[i] = (TypeHashCode)this.Deserialize(reader);
                }
            }

            return new TypeHashCode(hashcode, genericArgs ?? new TypeHashCode[0]);
        }

        public override void Serialize(DataWriter writer, object obj)
        {
            TypeHashCode typeHashCode = (TypeHashCode)obj;
            ulong hashcode = (ulong)typeHashCode;
            writer.Write(hashcode);

            if (typeHashCode.IsGenericType)
            {
                for (int i = 0; i < typeHashCode.GenericArgumentCount; i++)
                {
                    this.Serialize(writer, typeHashCode[i]);
                }
            }
        }
    }
}
