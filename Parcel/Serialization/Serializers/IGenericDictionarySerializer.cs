using Parcel.Lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Parcel.Serialization
{
    internal class IGenericDictionarySerializer : Serializer
    {
        private const byte KEY_TYPE_FLAG = 0b1000_0000;
        private const byte VALUE_TYPE_FLAG = 0b0100_0000;

        public override bool CanSerialize(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Dictionary<,>));
        }

        public override object Deserialize(ByteReader reader)
        {
            int count = reader.ReadInt();

            ObjectCache keyCache = ObjectCache.FromHash(ObjectCache.HashCode[0]);
            ObjectCache valueCache = ObjectCache.FromHash(ObjectCache.HashCode[1]);

            IDictionary dict = (IDictionary)Create.New(typeof(Dictionary<,>).MakeGenericType(keyCache.Type, valueCache.Type));
            
            for (int i = 0; i < count; i++)
            {
                int flag = reader.ReadByte();

                ObjectCache iKeyCache = (flag & KEY_TYPE_FLAG) == KEY_TYPE_FLAG ? ObjectCache.FromHash(reader.ReadTypeHashCode()) : keyCache;
                object key = reader.ReadWithoutTypeInfo(iKeyCache.Type);

                ObjectCache iValueCache = (flag & VALUE_TYPE_FLAG) == VALUE_TYPE_FLAG ? ObjectCache.FromHash(reader.ReadTypeHashCode()) : valueCache;
                object value = reader.ReadWithoutTypeInfo(iValueCache.Type);

                dict.Add(key, value);
            }

            return dict;
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            IDictionary dict = (IDictionary)obj;

            Type type = obj.GetType();
            Type[] genericArgs = type.GetGenericArguments();
            writer.Write(dict.Count);

            foreach (DictionaryEntry de in dict)
            {
                Type keyType = de.Key.GetType();
                Type valueType = de.Value.GetType();

                byte flag = 0;
                if (keyType != genericArgs[0])
                    flag |= KEY_TYPE_FLAG;
                if (valueType != genericArgs[1])
                    flag |= VALUE_TYPE_FLAG;
                writer.Write(flag);

                if ((flag & KEY_TYPE_FLAG) == KEY_TYPE_FLAG)
                    writer.Write(keyType.GetTypeHashCode());
                writer.WriteWithoutTypeInfo(de.Key);
                
                if ((flag & VALUE_TYPE_FLAG) == VALUE_TYPE_FLAG)
                    writer.Write(valueType.GetTypeHashCode());
                writer.WriteWithoutTypeInfo(de.Value);
            }

        }
    }
}
