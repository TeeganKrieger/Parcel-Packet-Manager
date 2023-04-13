using Parcel.Lib;
using System;

namespace Parcel.Serialization.Binary
{
    internal class ObjectSerializer : SerializerV2, IBinarySerializer
    {
        public override object Deserialize(DataReader reader)
        {
            object[] setterArgs = new object[1];

            object obj = Create.New(ObjectCache.Type);

            uint propertyHash = reader.ReadUInt();
            while (propertyHash != 0U)
            {
                ObjectProperty property = ObjectCache.GetProperty(propertyHash);
                bool readWithTypeHash = reader.ReadBoolean();
                setterArgs[0] = readWithTypeHash ? reader.ReadObject() : reader.ReadObject(false, property.Type);
                property.Setter.Invoke(obj, setterArgs);
                propertyHash = reader.ReadUInt();
            }

            return obj;
        }

        public override void Serialize(DataWriter writer, object obj)
        {
            object[] getterArgs = new object[0];

            foreach (ObjectProperty property in ObjectCache)
            {
                writer.Write(property.NameHash);
                object value = property.Getter.Invoke(obj, getterArgs);
                bool writeWithTypeHash = value == null ? false : value.GetType() != property.Type;
                writer.Write(writeWithTypeHash);
                if (writeWithTypeHash)
                    writer.Write(value);
                else
                    writer.Write(value, false);
            }

            writer.Write(0U);
        }

        public override bool CanSerialize(Type type)
        {
            return !type.IsPrimitive && type != typeof(string);
        }
    }
}
