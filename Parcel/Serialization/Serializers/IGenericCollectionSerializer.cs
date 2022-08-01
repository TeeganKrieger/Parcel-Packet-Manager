using Parcel.Lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Parcel.Serialization
{
    internal class IGenericCollectionSerializer : Serializer
    {
        private QuickDelegate AddDelegate { get; set; }
        private QuickDelegate GetEnumeratorDelegate { get; set; }

        public override bool CanSerialize(Type type)
        {
            if (!type.IsGenericType)
                return false;

            Type genericICollection = typeof(ICollection<>).MakeGenericType(type.GenericTypeArguments[0]);

            return genericICollection.IsAssignableFrom(type);
        }

        public override object Deserialize(ByteReader reader)
        {
            if (AddDelegate == null)
                AddDelegate = ObjectCache.Type.GetMethod("Add", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Bind();

            int count = reader.ReadInt();

            ObjectCache entryCache = ObjectCache.FromHash(ObjectCache.HashCode[0]);

            object collection = Create.New(ObjectCache.Type);
            object[] setterArgs = new object[1];

            for (int i = 0; i < count; i++)
            {
                int flag = reader.ReadByte();

                ObjectCache cache = flag == 1 ? ObjectCache.FromHash(reader.ReadTypeHashCode()) : entryCache;
                setterArgs[0] = reader.ReadObject(cache.Type);
                AddDelegate(collection, setterArgs);
            }

            return collection;
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            if (GetEnumeratorDelegate == null)
                GetEnumeratorDelegate = ObjectCache.Type.GetMethod("GetEnumerator", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Bind();

            Type type = ObjectCache.Type;
            Type[] genericArgs = type.GetGenericArguments();

            IEnumerator enumerator = (IEnumerator)GetEnumeratorDelegate(obj, new object[0]);

            int countPosition = writer.Position;
            int count = 0;
            writer.Write(0); //Write length

            while (enumerator.MoveNext())
            {
                count++;

                object entry = enumerator.Current;
                Type entryType = entry.GetType();

                byte flag = 0;
                if (entryType != genericArgs[0])
                    flag = 1;
                writer.Write(flag);

                if (flag == 1)
                    writer.Write(entryType.GetTypeHashCode());
                writer.Write(entry, false);
            }

            writer.Write(count, countPosition);
        }
    }
}
