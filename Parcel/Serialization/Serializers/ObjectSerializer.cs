using Parcel.Lib;
using System;
using System.Collections.Generic;
using System.Text;

namespace Parcel.Serialization
{
    internal class ObjectSerializer : Serializer
    {
        public override object Deserialize(ByteReader reader)
        {
            object[] setterArgs = new object[1];

            object obj = Create.New(ObjectCache.Type);

            uint propertyHash = reader.ReadUInt();
            while (propertyHash != 0U)
            {
                ObjectProperty property = ObjectCache[propertyHash];
                bool readWithTypeHash = reader.ReadBool();
                setterArgs[0] = readWithTypeHash ? reader.ReadObject() : reader.ReadObject(property.Type);
                property.Setter.Invoke(obj, setterArgs);
                propertyHash = reader.ReadUInt();
            }

            return obj;
        }

        public override void Serialize(ByteWriter writer, object obj)
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
