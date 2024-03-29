﻿using System;

namespace Parcel.Serialization
{
    internal class StringArraySerializer : Serializer
    {
        public override object Deserialize(ByteReader reader)
        {
            return reader.ReadStringArray();
        }

        public override void Serialize(ByteWriter writer, object obj)
        {
            writer.Write((string[])obj);
        }

        public override bool CanSerialize(Type type)
        {
            return type == typeof(string[]);
        }
    }
}
